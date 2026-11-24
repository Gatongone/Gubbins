using System.Runtime.CompilerServices;

// The translator keeps the entry point that reads a source text unit tested: it is how the body rewriting is proved
// without a device, and it is not the way a program translates a kernel anymore. The semantic entry point is the public
// one; this makes the text one visible to the tests alone.
[assembly: InternalsVisibleTo("Gubbins.Graphic.Tests")]
