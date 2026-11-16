namespace Gubbins.Unsafe.Tests;

[TestFixture]
public sealed unsafe class AllocatorTests
{
    private static void AssertAligned(void* pointer, byte alignment)
    {
        Assert.That(((nuint)pointer & (nuint)(alignment - 1)) == 0, Is.True);
    }

    private static void Fill(byte* pointer, int length, byte value)
    {
        for (var i = 0; i < length; i++)
        {
            pointer[i] = value;
        }
    }


    [Test]
    public void FixedAllocator_Alloc_ReturnsAlignedPointers()
    {
        var buffer = new byte[1024];
        var impl = new FixedAllocator(buffer);
        var allocator = impl.GetAllocator();

        // Start at a deliberately unaligned offset.
        allocator.Alloc(1, 1);

        for (byte alignment = 2; alignment <= 64; alignment <<= 1)
        {
            var pointer = (byte*)allocator.Alloc(3, alignment);
            AssertAligned(pointer, alignment);
            Fill(pointer, 3, 0xAB);
            Assert.That(pointer[2], Is.EqualTo(0xAB));
        }
    }

    [Test]
    public void FixedAllocator_Alloc_BeyondCapacity_Throws()
    {
        var buffer = new byte[64];
        var impl = new FixedAllocator(buffer);
        var allocator = impl.GetAllocator();

        allocator.Alloc(64, 1);

        Assert.Throws<OutOfMemoryException>(() => allocator.Alloc(1, 1));
    }

    [Test]
    public void FixedAllocator_Alloc_ZeroLength_DoesNotMoveCursor()
    {
        var buffer = new byte[64];
        var impl = new FixedAllocator(buffer);
        var allocator = impl.GetAllocator();

        var pointer = allocator.Alloc(0, 8);

        Assert.That(impl.Length, Is.EqualTo(0));
        Assert.That((nuint)pointer, Is.Not.Zero);
    }

    [Test]
    public void FixedAllocator_Resize_OnlyLastBlock()
    {
        var buffer = new byte[256];
        var impl = new FixedAllocator(buffer);
        var allocator = impl.GetAllocator();

        var first = allocator.Alloc(16, 1);
        var second = allocator.Alloc(16, 1);

        Assert.That(allocator.Resize(first, 16, 32, 1), Is.False);
        Assert.That(allocator.Resize(second, 16, 32, 1), Is.True);
        Assert.That(impl.Length, Is.EqualTo(48));
        Assert.That(allocator.Resize(second, 32, 8, 1), Is.True);
        Assert.That(impl.Length, Is.EqualTo(24));
    }

    [Test]
    public void FixedAllocator_Resize_BeyondCapacity_Fails()
    {
        var buffer = new byte[32];
        var impl = new FixedAllocator(buffer);
        var allocator = impl.GetAllocator();

        var pointer = allocator.Alloc(16, 1);

        Assert.That(allocator.Resize(pointer, 16, 64, 1), Is.False);
        Assert.That(impl.Length, Is.EqualTo(16));
    }

    [Test]
    public void FixedAllocator_Free_RewindsOnlyLastBlock()
    {
        var buffer = new byte[256];
        var impl = new FixedAllocator(buffer);
        var allocator = impl.GetAllocator();

        var first = allocator.Alloc(16, 1);
        var second = allocator.Alloc(16, 1);

        allocator.Free(first, 16, 1);
        Assert.That(impl.Length, Is.EqualTo(32));

        allocator.Free(second, 16, 1);
        Assert.That(impl.Length, Is.EqualTo(16));

        var again = allocator.Alloc(16, 1);
        Assert.That((nuint)again, Is.EqualTo((nuint)second));
    }

    [Test]
    public void FixedAllocator_Remap_ShrinkKeepsPointer_GrowOnlyLast()
    {
        var buffer = new byte[256];
        var impl = new FixedAllocator(buffer);
        var allocator = impl.GetAllocator();

        var first = allocator.Alloc(16, 1);
        var second = allocator.Alloc(16, 1);

        Assert.That((nuint)allocator.Remap(first, 16, 8, 1), Is.EqualTo((nuint)first));
        Assert.That(allocator.Remap(first, 16, 32, 1) == null, Is.True);
        Assert.That((nuint)allocator.Remap(second, 16, 32, 1), Is.EqualTo((nuint)second));
    }

    [Test]
    public void FixedAllocator_ResetAndDispose_Rewind()
    {
        var buffer = new byte[256];
        var impl = new FixedAllocator(buffer);
        var allocator = impl.GetAllocator();

        allocator.Alloc(64, 1);
        allocator.Reset();
        Assert.That(impl.Length, Is.EqualTo(0));

        allocator.Alloc(64, 1);
        allocator.Dispose();
        Assert.That(impl.Length, Is.EqualTo(0));
    }


    [Test]
    public void ArenaAllocator_Alloc_AcrossGrowth_PreservesContentsAndAligns()
    {
        var impl = new ArenaAllocator(UnmanagedAllocator.Global);
        var allocator = impl.GetAllocator();

        try
        {
            var first = (byte*)allocator.Alloc(100, 1);
            var second = (byte*)allocator.Alloc(100, 8);

            Fill(first, 100, 0x11);
            Fill(second, 100, 0x22);

            AssertAligned(second, 8);

            // Forces the arena to acquire and chain additional blocks.
            var large = (byte*)allocator.Alloc(1_000_000, 16);
            AssertAligned(large, 16);
            Fill(large, 1_000_000, 0x33);

            for (var i = 0; i < 100; i++)
            {
                Assert.That(first[i], Is.EqualTo(0x11));
                Assert.That(second[i], Is.EqualTo(0x22));
            }
        }
        finally
        {
            allocator.Dispose();
        }
    }

    [Test]
    public void ArenaAllocator_Resize_OnlyLastAllocation()
    {
        var impl = new ArenaAllocator(UnmanagedAllocator.Global);
        var allocator = impl.GetAllocator();

        try
        {
            var first = allocator.Alloc(64, 8);
            var second = allocator.Alloc(64, 8);

            Assert.That(allocator.Resize(first, 64, 128, 8), Is.False);
            Assert.That(allocator.Resize(second, 64, 128, 8), Is.True);

            Fill((byte*)second, 128, 0x44);
            Assert.That(allocator.Resize(second, 128, 32, 8), Is.True);
        }
        finally
        {
            allocator.Dispose();
        }
    }

    [Test]
    public void ArenaAllocator_Free_RewindsLastAllocation()
    {
        var impl = new ArenaAllocator(UnmanagedAllocator.Global);
        var allocator = impl.GetAllocator();

        try
        {
            var first = allocator.Alloc(32, 8);
            var second = allocator.Alloc(32, 8);

            allocator.Free(first, 32, 8);
            allocator.Free(second, 32, 8);

            var again = allocator.Alloc(32, 8);
            Assert.That((nuint)again, Is.EqualTo((nuint)second));
        }
        finally
        {
            allocator.Dispose();
        }
    }

    [Test]
    public void ArenaAllocator_Reset_KeepsHeadBlock_AndRemainsUsable()
    {
        var impl = new ArenaAllocator(UnmanagedAllocator.Global);
        var allocator = impl.GetAllocator();

        try
        {
            var first = allocator.Alloc(128, 8);
            Fill((byte*)first, 128, 0x55);

            allocator.Reset();

            var second = (byte*)allocator.Alloc(128, 8);
            Assert.That((nuint)second, Is.Not.Zero);
            Fill(second, 128, 0x66);
            Assert.That(second[127], Is.EqualTo(0x66));
        }
        finally
        {
            allocator.Dispose();
        }
    }

    [Test]
    public void ArenaAllocator_Dispose_FreesAll_AndRemainsUsable()
    {
        var impl = new ArenaAllocator(UnmanagedAllocator.Global);
        var allocator = impl.GetAllocator();

        try
        {
            allocator.Alloc(256, 8);
            allocator.Dispose();

            var again = (byte*)allocator.Alloc(256, 8);
            Assert.That((nuint)again, Is.Not.Zero);
            Fill(again, 256, 0x77);
        }
        finally
        {
            allocator.Dispose();
        }
    }


    [Test]
    public void UnmanagedAllocator_Alloc_RespectsAlignment_AndIsWritable()
    {
        var impl = new UnmanagedAllocator();
        var allocator = impl.GetAllocator();

        var pointer = (byte*)allocator.Alloc(128, 64);
        try
        {
            AssertAligned(pointer, 64);
            Fill(pointer, 128, 0x99);
            Assert.That(pointer[127], Is.EqualTo(0x99));
        }
        finally
        {
            allocator.Free(pointer, 128, 64);
        }
    }

    [Test]
    public void UnmanagedAllocator_Resize_IsNotSupported()
    {
        var impl = new UnmanagedAllocator();
        var allocator = impl.GetAllocator();

        var pointer = allocator.Alloc(64, 8);
        try
        {
            Assert.That(allocator.Resize(pointer, 64, 128, 8), Is.False);
        }
        finally
        {
            allocator.Free(pointer, 64, 8);
        }
    }

    [Test]
    public void UnmanagedAllocator_Remap_PreservesContents()
    {
        var impl = new UnmanagedAllocator();
        var allocator = impl.GetAllocator();

        var pointer = (byte*)allocator.Alloc(64, 8);
        Fill(pointer, 64, 0xAA);

        var remapped = (byte*)allocator.Remap(pointer, 64, 256, 8);
        try
        {
            Assert.That((nuint)remapped, Is.Not.Zero);
            AssertAligned(remapped, 8);
            for (var i = 0; i < 64; i++)
            {
                Assert.That(remapped[i], Is.EqualTo(0xAA));
            }

            Fill(remapped + 64, 192, 0xBB);
        }
        finally
        {
            allocator.Free(remapped, 256, 8);
        }
    }

    [Test]
    public void UnmanagedAllocator_ResetAndDispose_AreNoOps()
    {
        var impl = new UnmanagedAllocator();
        var allocator = impl.GetAllocator();

        allocator.Reset();
        allocator.Dispose();

        var pointer = allocator.Alloc(32, 8);
        Assert.That((nuint)pointer, Is.Not.Zero);
        allocator.Free(pointer, 32, 8);
    }


    [Test]
    public void ManagedAllocator_Alloc_AndFree_AreWritable()
    {
        var impl = new ManagedAllocator();
        var allocator = impl.GetAllocator();

        try
        {
            var pointer = (byte*)allocator.Alloc(64, 8);
            Fill(pointer, 64, 0xCC);
            Assert.That(pointer[63], Is.EqualTo(0xCC));

            allocator.Free(pointer, 64, 8);

            var address = (nint)pointer;
            Assert.Throws<InvalidOperationException>(() => allocator.Free((void*)address, 64, 8));
        }
        finally
        {
            allocator.Dispose();
        }
    }

    [Test]
    public void ManagedAllocator_Remap_PreservesContents()
    {
        var impl = new ManagedAllocator();
        var allocator = impl.GetAllocator();

        try
        {
            var pointer = (byte*)allocator.Alloc(64, 8);
            Fill(pointer, 64, 0xDD);

            var remapped = (byte*)allocator.Remap(pointer, 64, 256, 8);
            for (var i = 0; i < 64; i++)
            {
                Assert.That(remapped[i], Is.EqualTo(0xDD));
            }

            allocator.Free(remapped, 256, 8);
        }
        finally
        {
            allocator.Dispose();
        }
    }

    [Test]
    public void ManagedAllocator_Resize_IsNotSupported()
    {
        var impl = new ManagedAllocator();
        var allocator = impl.GetAllocator();

        try
        {
            var pointer = allocator.Alloc(32, 8);
            Assert.That(allocator.Resize(pointer, 32, 64, 8), Is.False);
            allocator.Free(pointer, 32, 8);
        }
        finally
        {
            allocator.Dispose();
        }
    }

    [Test]
    public void ManagedAllocator_Dispose_ReleasesPins_AndRemainsUsable()
    {
        var impl = new ManagedAllocator();
        var allocator = impl.GetAllocator();

        try
        {
            allocator.Alloc(32, 8);
            allocator.Dispose();

            var again = (byte*)allocator.Alloc(32, 8);
            Assert.That((nuint)again, Is.Not.Zero);
            Fill(again, 32, 0xEE);
        }
        finally
        {
            allocator.Dispose();
        }
    }


    [Test]
    public void VirtualAllocator_Alloc_RespectsAlignment_AndIsWritable()
    {
        var impl = new VirtualAllocator(1 << 20);
        var allocator = impl.GetAllocator();

        try
        {
            var pointer = (byte*)allocator.Alloc(128, 64);
            AssertAligned(pointer, 64);
            Fill(pointer, 128, 0x12);
            Assert.That(pointer[127], Is.EqualTo(0x12));
        }
        finally
        {
            allocator.Dispose();
        }
    }

    [Test]
    public void VirtualAllocator_Resize_AcrossPages_PreservesContents()
    {
        var impl = new VirtualAllocator(1 << 20);
        var allocator = impl.GetAllocator();
        var newLength = Environment.SystemPageSize * 2 + 64;

        try
        {
            var pointer = (byte*)allocator.Alloc(64, 8);
            Fill(pointer, 64, 0x23);

            Assert.That(allocator.Resize(pointer, 64, (nuint)newLength, 8), Is.True);

            for (var i = 0; i < 64; i++)
            {
                Assert.That(pointer[i], Is.EqualTo(0x23));
            }

            Fill(pointer + 64, newLength - 64, 0x34);
            Assert.That(pointer[newLength - 1], Is.EqualTo(0x34));
        }
        finally
        {
            allocator.Dispose();
        }
    }

    [Test]
    public void VirtualAllocator_Resize_OnlyLastAllocation()
    {
        var impl = new VirtualAllocator(1 << 20);
        var allocator = impl.GetAllocator();

        try
        {
            var first = allocator.Alloc(64, 8);
            var second = allocator.Alloc(64, 8);

            Assert.That(allocator.Resize(first, 64, 128, 8), Is.False);
            Assert.That(allocator.Resize(second, 64, 128, 8), Is.True);
        }
        finally
        {
            allocator.Dispose();
        }
    }

    [Test]
    public void VirtualAllocator_Reset_KeepsReservation_AndRemainsUsable()
    {
        var impl = new VirtualAllocator(1 << 20);
        var allocator = impl.GetAllocator();

        try
        {
            var first = (byte*)allocator.Alloc(128, 8);
            Fill(first, 128, 0x45);

            allocator.Reset();

            var second = (byte*)allocator.Alloc(128, 8);
            Assert.That((nuint)second, Is.Not.Zero);
            Fill(second, 128, 0x56);
            Assert.That(second[127], Is.EqualTo(0x56));
        }
        finally
        {
            allocator.Dispose();
        }
    }

    [Test]
    public void VirtualAllocator_Dispose_ReleasesReservation_AndRemainsUsable()
    {
        var impl = new VirtualAllocator(1 << 20);
        var allocator = impl.GetAllocator();

        try
        {
            allocator.Alloc(128, 8);
            allocator.Dispose();

            var again = (byte*)allocator.Alloc(128, 8);
            Assert.That((nuint)again, Is.Not.Zero);
            Fill(again, 128, 0x67);
        }
        finally
        {
            allocator.Dispose();
        }
    }

    [Test]
    public void VirtualAllocator_Alloc_BeyondReservation_Throws()
    {
        var impl = new VirtualAllocator(4096);
        var allocator = impl.GetAllocator();

        try
        {
            Assert.Throws<OutOfMemoryException>(() => allocator.Alloc(8192, 8));
        }
        finally
        {
            allocator.Dispose();
        }
    }


    [Test]
    public void DefaultAllocator_IsInvalid_AndDisposeAndReset_AreNoOps()
    {
        var allocator = default(Allocator);

        Assert.That(allocator.IsValid, Is.False);
        Assert.DoesNotThrow(() => allocator.Dispose());
        Assert.DoesNotThrow(() => allocator.Reset());
    }

    [Test]
    public void AllocatorExtensions_AllocAndFree_TypedMemory()
    {
        var impl = new UnmanagedAllocator();
        var allocator = impl.GetAllocator();

        var values = allocator.Alloc<int>(8);
        try
        {
            Assert.That(values.Length, Is.EqualTo(8));
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = i * 7;
            }

            for (var i = 0; i < values.Length; i++)
            {
                Assert.That(values[i], Is.EqualTo(i * 7));
            }
        }
        finally
        {
            allocator.Free(values);
        }
    }

    [Test]
    public void AllocatorExtensions_NewAndFree_TypedValue()
    {
        var impl = new UnmanagedAllocator();
        var allocator = impl.GetAllocator();

        ref var value = ref allocator.New<int>();
        try
        {
            value = 123;
            Assert.That(value, Is.EqualTo(123));
        }
        finally
        {
            allocator.Free(ref value);
        }
    }

    [Test]
    public void AllocatorExtensions_Resize_LastTypedBlock()
    {
        var buffer = new byte[1024];
        var impl = new FixedAllocator(buffer);
        var allocator = impl.GetAllocator();

        var values = allocator.Alloc<int>(8);
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = i + 1;
        }

        Assert.That(allocator.Resize(values, 16, out var resized), Is.True);
        Assert.That(resized.Length, Is.EqualTo(16));
        for (var i = 0; i < 8; i++)
        {
            Assert.That(resized[i], Is.EqualTo(i + 1));
        }

        allocator.Free(resized);
    }

    [Test]
    public void AllocatorExtensions_Remap_Unmanaged_PreservesContents()
    {
        var impl = new UnmanagedAllocator();
        var allocator = impl.GetAllocator();

        var values = allocator.Alloc<int>(4);
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = 100 + i;
        }

        var remapped = allocator.Remap(values, 16);
        try
        {
            Assert.That(remapped.Length, Is.EqualTo(16));
            for (var i = 0; i < 4; i++)
            {
                Assert.That(remapped[i], Is.EqualTo(100 + i));
            }
        }
        finally
        {
            allocator.Free(remapped);
        }
    }

    [Test]
    public void AllocatorExtensions_Remap_WhenBlockCannotMove_ReturnsEmpty()
    {
        var buffer = new byte[64];
        var impl = new FixedAllocator(buffer);
        var allocator = impl.GetAllocator();

        var values = allocator.Alloc<int>(4);
        var remapped = allocator.Remap(values, 1024);

        Assert.That(remapped.IsEmpty, Is.True);
        allocator.Free(values);
    }

    // ---------------------------------------------------------------- Additional edge cases

    [Test]
    public void FixedAllocator_Properties_ReflectAllocationState()
    {
        var buffer = new byte[32];
        var impl = new FixedAllocator(buffer);

        Assert.That(impl.Capacity, Is.EqualTo(32));
        Assert.That(impl.Full, Is.False);

        var allocator = impl.GetAllocator();
        allocator.Alloc(32, 1);

        Assert.That(impl.Length, Is.EqualTo(32));
        Assert.That(impl.Full, Is.True);
    }

    [Test]
    public void FixedAllocator_ForeignPointers_DoNotAffectState()
    {
        var buffer = new byte[64];
        var impl = new FixedAllocator(buffer);
        var allocator = impl.GetAllocator();

        var pointer = allocator.Alloc(16, 1);
        var foreign = (void*)0x1000;

        allocator.Free(foreign, 16, 1);
        Assert.That(impl.Length, Is.EqualTo(16));

        Assert.That(allocator.Resize(foreign, 16, 32, 1), Is.False);
        Assert.That(impl.Length, Is.EqualTo(16));
        Assert.That((nuint)pointer, Is.Not.Zero);
    }

    [Test]
    public void Allocator_Cast_ReturnsOwningImplementation()
    {
        var impl = new UnmanagedAllocator();
        var allocator = impl.GetAllocator();

        ref var cast = ref allocator.Cast<UnmanagedAllocator>();

        Assert.That((nuint)cast.GetAllocator().Context, Is.EqualTo((nuint)allocator.Context));

        var pointer = cast.GetAllocator().Alloc(16, 8);
        cast.GetAllocator().Free(pointer, 16, 8);
    }

    [Test]
    public void ArenaAllocator_Remap_ShrinkAndGrowLastAllocation()
    {
        var impl = new ArenaAllocator(UnmanagedAllocator.Global);
        var allocator = impl.GetAllocator();

        try
        {
            var first = (byte*)allocator.Alloc(64, 8);
            Fill(first, 64, 0x7A);

            Assert.That((nuint)allocator.Remap(first, 64, 32, 8), Is.EqualTo((nuint)first));

            var grown = (byte*)allocator.Remap(first, 64, 128, 8);
            Assert.That((nuint)grown, Is.EqualTo((nuint)first));
            for (var i = 0; i < 64; i++)
            {
                Assert.That(grown[i], Is.EqualTo(0x7A));
            }

            allocator.Alloc(64, 8);
            Assert.That(allocator.Remap(first, 128, 256, 8) == null, Is.True);
        }
        finally
        {
            allocator.Dispose();
        }
    }

    [Test]
    public void ArenaAllocator_EmptyResetAndDispose_AreSafe()
    {
        var impl = new ArenaAllocator(UnmanagedAllocator.Global);
        var allocator = impl.GetAllocator();

        allocator.Reset();
        allocator.Dispose();

        var pointer = allocator.Alloc(64, 8);
        Assert.That((nuint)pointer, Is.Not.Zero);

        allocator.Dispose();
    }

    [Test]
    public void ArenaAllocator_Alloc_TooLarge_Throws()
    {
        var impl = new ArenaAllocator(UnmanagedAllocator.Global);
        var allocator = impl.GetAllocator();

        try
        {
            Assert.Throws<OutOfMemoryException>(() => allocator.Alloc(0x7FFFFFC8, 8));
        }
        finally
        {
            allocator.Dispose();
        }
    }

    [Test]
    public void ArenaAllocator_ForeignPointer_IsIgnored()
    {
        var impl = new ArenaAllocator(UnmanagedAllocator.Global);
        var allocator = impl.GetAllocator();

        try
        {
            var pointer = allocator.Alloc(32, 8);

            allocator.Free((void*)0x1000, 32, 8);
            Assert.That(allocator.Resize((void*)0x1000, 32, 64, 8), Is.False);
            Assert.That((nuint)pointer, Is.Not.Zero);
        }
        finally
        {
            allocator.Dispose();
        }
    }

    [Test]
    public void VirtualAllocator_Free_RewindsLastAllocation()
    {
        var impl = new VirtualAllocator(1 << 20);
        var allocator = impl.GetAllocator();

        try
        {
            var first = (byte*)allocator.Alloc(64, 8);
            var second = (byte*)allocator.Alloc(64, 8);

            allocator.Free(first, 64, 8);   // Older block: ignored.
            allocator.Free(second, 64, 8);  // Last block: rewind.

            var again = (byte*)allocator.Alloc(64, 8);
            Assert.That((nuint)again, Is.EqualTo((nuint)second));
        }
        finally
        {
            allocator.Dispose();
        }
    }

    [Test]
    public void VirtualAllocator_Free_ForeignPointer_IsIgnored()
    {
        var impl = new VirtualAllocator(1 << 20);
        var allocator = impl.GetAllocator();

        try
        {
            var pointer = allocator.Alloc(64, 8);

            allocator.Free((void*)0x1000, 64, 8);
            Assert.That(allocator.Resize((void*)0x1000, 64, 128, 8), Is.False);
            Assert.That((nuint)pointer, Is.Not.Zero);
        }
        finally
        {
            allocator.Dispose();
        }
    }

    [Test]
    public void VirtualAllocator_Remap_ShrinkKeepsPointer_GrowOnlyLast()
    {
        var impl = new VirtualAllocator(1 << 20);
        var allocator = impl.GetAllocator();

        try
        {
            var first = (byte*)allocator.Alloc(64, 8);
            Fill(first, 64, 0x31);

            Assert.That((nuint)allocator.Remap(first, 64, 32, 8), Is.EqualTo((nuint)first));

            var grown = (byte*)allocator.Remap(first, 64, 128, 8);
            Assert.That((nuint)grown, Is.EqualTo((nuint)first));
            for (var i = 0; i < 64; i++)
            {
                Assert.That(grown[i], Is.EqualTo(0x31));
            }

            allocator.Alloc(64, 8);
            Assert.That(allocator.Remap(first, 128, 256, 8) == null, Is.True);
        }
        finally
        {
            allocator.Dispose();
        }
    }

    [Test]
    public void VirtualAllocator_ResetAndResizeWithoutAllocation_AreSafe()
    {
        var impl = new VirtualAllocator(1 << 20);
        var allocator = impl.GetAllocator();

        allocator.Reset();
        Assert.That(allocator.Resize((void*)0, 0, 16, 8), Is.False);

        var pointer = allocator.Alloc(64, 8);
        Assert.That((nuint)pointer, Is.Not.Zero);

        allocator.Dispose();
    }

    [Test]
    public void ManagedAllocator_FreeAndRemap_UnknownAddress_Throw()
    {
        var impl = new ManagedAllocator();
        var allocator = impl.GetAllocator();
        var address = (nint)0x1000;

        try
        {
            Assert.Throws<InvalidOperationException>(() => allocator.Free((void*)address, 16, 8));
            Assert.Throws<InvalidOperationException>(() => allocator.Remap((void*)address, 16, 32, 8));
        }
        finally
        {
            allocator.Dispose();
        }
    }

    [Test]
    public void ManagedAllocator_TooLargeRequests_Throw()
    {
        var impl = new ManagedAllocator();
        var allocator = impl.GetAllocator();
        var tooLarge = (nuint)int.MaxValue + 1;

        try
        {
            Assert.Throws<OutOfMemoryException>(() => allocator.Alloc(tooLarge, 8));

            var pointer = allocator.Alloc(16, 8);
            var address = (nint)pointer;
            Assert.Throws<OutOfMemoryException>(() => allocator.Remap((void*)address, 16, tooLarge, 8));
            allocator.Free(pointer, 16, 8);
        }
        finally
        {
            allocator.Dispose();
        }
    }

    [Test]
    public void AllocatorExtensions_Alloc_WithExplicitAlignment_AndFree()
    {
        var impl = new UnmanagedAllocator();
        var allocator = impl.GetAllocator();

        var values = allocator.Alloc<int>(4, 64);
        try
        {
            Assert.That(((nuint)Native.AsPointer(values) & 63) == 0, Is.True);

            values[0] = 5;
            Assert.That(values[0], Is.EqualTo(5));
        }
        finally
        {
            allocator.Free(values, 64);
        }
    }

    [Test]
    public void AllocatorExtensions_NewWithAlignment_AndFree()
    {
        var impl = new UnmanagedAllocator();
        var allocator = impl.GetAllocator();

        ref var value = ref allocator.New<int>(64);
        try
        {
            value = 11;
            Assert.That(value, Is.EqualTo(11));
        }
        finally
        {
            allocator.Free(ref value, 64);
        }
    }

    [Test]
    public void AllocatorExtensions_Resize_UnsupportedAllocator_ReturnsFalseAndEmptySpan()
    {
        var impl = new UnmanagedAllocator();
        var allocator = impl.GetAllocator();

        var values = allocator.Alloc<int>(4);
        try
        {
            Assert.That(allocator.Resize(values, 8, out var resized), Is.False);
            Assert.That(resized.IsEmpty, Is.True);
        }
        finally
        {
            allocator.Free(values);
        }
    }

    [Test]
    public void AllocatorExtensions_Resize_NegativeSize_Throws()
    {
        var buffer   = new byte[64];
        var impl     = new FixedAllocator(buffer);
        var allocator = impl.GetAllocator();
        var values   = allocator.Alloc<int>(4);

        var thrown = false;
        try
        {
            allocator.Resize(values, -1, out _);
        }
        catch (ArgumentOutOfRangeException)
        {
            thrown = true;
        }

        Assert.That(thrown, Is.True);
        allocator.Free(values);
    }

    [Test]
    public void AllocatorExtensions_Remap_WithExplicitAlignment_PreservesContents()
    {
        var impl = new UnmanagedAllocator();
        var allocator = impl.GetAllocator();

        var values = allocator.Alloc<int>(4, 64);
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = 200 + i;
        }

        var remapped = allocator.Remap(values, 8, 64);
        try
        {
            Assert.That(remapped.Length, Is.EqualTo(8));
            for (var i = 0; i < 4; i++)
            {
                Assert.That(remapped[i], Is.EqualTo(200 + i));
            }
        }
        finally
        {
            allocator.Free(remapped, 64);
        }
    }

    [Test]
    public void AllocatorExtensions_Realloc_TypedSpan_PreservesContents()
    {
        var impl = new UnmanagedAllocator();
        var allocator = impl.GetAllocator();

        var values = allocator.Alloc<int>(4);
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = 300 + i;
        }

        // Realloc uses the requested length as both the old and new length, so request the same size.
        var reallocated = allocator.Realloc(values);
        try
        {
            for (var i = 0; i < 4; i++)
            {
                Assert.That(reallocated[i], Is.EqualTo(300 + i));
            }
        }
        finally
        {
            allocator.Free(reallocated);
        }
    }

    [Test]
    public void ArenaAllocator_ResizeAndFree_OnEmptyArena_FailSafely()
    {
        var impl = new ArenaAllocator(UnmanagedAllocator.Global);
        var allocator = impl.GetAllocator();

        Assert.That(allocator.Resize((void*)0, 0, 16, 8), Is.False);
        allocator.Free((void*)0, 0, 1);

        allocator.Dispose();
    }

    [Test]
    public void ArenaAllocator_Resize_BeyondActiveBlock_Fails()
    {
        var impl = new ArenaAllocator(UnmanagedAllocator.Global);
        var allocator = impl.GetAllocator();

        try
        {
            var pointer = allocator.Alloc(64, 8);
            Assert.That(allocator.Resize(pointer, 64, 8192, 8), Is.False);
        }
        finally
        {
            allocator.Dispose();
        }
    }

    [Test]
    public void ArenaAllocator_Reset_ReleasesRetiredBlocks()
    {
        var impl = new ArenaAllocator(UnmanagedAllocator.Global);
        var allocator = impl.GetAllocator();

        try
        {
            var first = (byte*)allocator.Alloc(64, 8);
            Fill(first, 64, 0x12);

            // A second, much larger block makes the first block a retired block.
            allocator.Alloc(1_000_000, 8);

            allocator.Reset();

            var again = (byte*)allocator.Alloc(64, 8);
            Assert.That((nuint)again, Is.Not.Zero);
            Fill(again, 64, 0x34);
            Assert.That(again[63], Is.EqualTo(0x34));
        }
        finally
        {
            allocator.Dispose();
        }
    }

    [Test]
    public void ArenaAllocator_Alloc_GrowsActiveBlockThroughBaseAllocator()
    {
        var buffer = new byte[1 << 20];
        var baseImpl = new FixedAllocator(buffer);
        var impl = new ArenaAllocator(baseImpl.GetAllocator());
        var allocator = impl.GetAllocator();

        try
        {
            var first = (byte*)allocator.Alloc(64, 8);
            Fill(first, 64, 0x56);

            // Does not fit the initial 4 KiB arena block; the arena asks the fixed base allocator to grow it.
            var second = (byte*)allocator.Alloc(8192, 8);
            Assert.That((nuint)second, Is.Not.Zero);

            for (var i = 0; i < 64; i++)
            {
                Assert.That(first[i], Is.EqualTo(0x56));
            }
        }
        finally
        {
            allocator.Dispose();
        }
    }

    [Test]
    public void VirtualAllocator_Alloc_WhenReservationFails_Throws()
    {
        var impl = new VirtualAllocator(0);
        var allocator = impl.GetAllocator();

        try
        {
            Assert.Throws<OutOfMemoryException>(() => allocator.Alloc(16, 8));
        }
        finally
        {
            allocator.Dispose();
        }
    }

    [Test]
    public void VirtualAllocator_FreeWithoutReservation_IsNoOp()
    {
        var impl = new VirtualAllocator(4096);
        var allocator = impl.GetAllocator();

        allocator.Free((void*)0, 0, 1);
        allocator.Dispose();
    }

    [Test]
    public void VirtualAllocator_Resize_BeyondReservation_Fails()
    {
        var impl = new VirtualAllocator(4096);
        var allocator = impl.GetAllocator();

        try
        {
            var pointer = allocator.Alloc(64, 8);
            Assert.That(allocator.Resize(pointer, 64, 8192, 8), Is.False);
        }
        finally
        {
            allocator.Dispose();
        }
    }

    [Test]
    public void AllocatorExtensions_Alloc_TooManyElements_Throws()
    {
        var impl = new UnmanagedAllocator();
        var allocator = impl.GetAllocator();
        var tooMany = (nuint)int.MaxValue + 1;

        Assert.Throws<ArgumentOutOfRangeException>(() => allocator.Alloc<int>(tooMany));
    }

    [Test]
    public void AllocatorExtensions_Remap_TooManyElements_Throws()
    {
        var impl = new UnmanagedAllocator();
        var allocator = impl.GetAllocator();

        var values = allocator.Alloc<int>(2, 4);
        var tooMany = (nuint)int.MaxValue + 1;

        var thrown = false;
        try
        {
            allocator.Remap(values, tooMany, 4);
        }
        catch (ArgumentOutOfRangeException)
        {
            thrown = true;
        }

        Assert.That(thrown, Is.True);
        allocator.Free(values, 4);
    }

    [Test]
    public void AllocatorExtensions_Realloc_Untyped_LastBlockReturnsSamePointer()
    {
        var buffer = new byte[64];
        var impl = new FixedAllocator(buffer);
        var allocator = impl.GetAllocator();

        var pointer = allocator.Alloc(16, 1);
        var reallocated = allocator.Realloc(pointer, 16, 1);

        Assert.That((nuint)reallocated, Is.EqualTo((nuint)pointer));

        allocator.Free(reallocated, 16, 1);
    }

    [Test]
    public void AllocatorExtensions_Realloc_TypedRef_DefaultAlignment()
    {
        var impl = new UnmanagedAllocator();
        var allocator = impl.GetAllocator();

        ref var value = ref allocator.New<int>();
        value = 77;

        ref var reallocated = ref allocator.Realloc(ref value, (nuint)sizeof(int));
        try
        {
            Assert.That(reallocated, Is.EqualTo(77));
        }
        finally
        {
            allocator.Free(ref reallocated);
        }
    }

    [Test]
    public void AllocatorExtensions_Realloc_TypedRef_WithAlignment()
    {
        var impl = new UnmanagedAllocator();
        var allocator = impl.GetAllocator();

        ref var value = ref allocator.New<int>(4);
        value = 88;

        ref var reallocated = ref allocator.Realloc(ref value, (nuint)sizeof(int), 4);
        try
        {
            Assert.That(reallocated, Is.EqualTo(88));
        }
        finally
        {
            allocator.Free(ref reallocated, 4);
        }
    }

    // ---------------------------------------------------------------- AllocatorHandle

    [Test]
    public void AllocatorHandle_UnmanagedAllocator_AllocAndFree()
    {
        var handle = AllocatorHandle.Create(new UnmanagedAllocator());

        try
        {
            Assert.That(handle.IsValid, Is.True);
            Assert.That(handle.Allocator.IsValid, Is.True);

            var pointer = (byte*)handle.Allocator.Alloc(64, 16);
            AssertAligned(pointer, 16);
            Fill(pointer, 64, 0x42);
            Assert.That(pointer[63], Is.EqualTo(0x42));

            handle.Allocator.Free(pointer, 64, 16);
        }
        finally
        {
            handle.Dispose();
        }

        Assert.That(handle.IsValid, Is.False);
    }

    [Test]
    public void AllocatorHandle_ArenaAllocator_ResetAndDispose()
    {
        var baseImpl = new UnmanagedAllocator();
        var handle = AllocatorHandle.Create(new ArenaAllocator(baseImpl.GetAllocator()));

        try
        {
            var first = (byte*)handle.Allocator.Alloc(128, 8);
            Fill(first, 128, 0x24);

            handle.Reset();

            var second = (byte*)handle.Allocator.Alloc(128, 8);
            Assert.That((nuint)second, Is.Not.Zero);
            Fill(second, 128, 0x68);
            Assert.That(second[127], Is.EqualTo(0x68));
        }
        finally
        {
            handle.Dispose();
        }
    }

    [Test]
    public void AllocatorHandle_VirtualAllocator_AllocAndDispose()
    {
        var handle = AllocatorHandle.Create(new VirtualAllocator(1 << 20));

        try
        {
            var pointer = (byte*)handle.Allocator.Alloc(128, 32);
            AssertAligned(pointer, 32);
            Fill(pointer, 128, 0x86);
            Assert.That(pointer[127], Is.EqualTo(0x86));
        }
        finally
        {
            handle.Dispose();
        }
    }

    [Test]
    public void AllocatorHandle_Dispose_IsIdempotent()
    {
        var handle = AllocatorHandle.Create(new UnmanagedAllocator());

        handle.Dispose();
        Assert.That(handle.IsValid, Is.False);

        Assert.DoesNotThrow(() => handle.Dispose());
        Assert.That(handle.IsValid, Is.False);
    }
}