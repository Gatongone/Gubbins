namespace Gubbins.Network;

/// <summary>
/// Character set mime names.
/// </summary>
// ReSharper disable All
public struct HttpCharsetType
{
    private readonly string m_Charset;
    public HttpCharsetType(string charset) => m_Charset = charset;
    public static implicit operator string(HttpCharsetType contentType) => contentType.m_Charset;
    public static implicit operator HttpCharsetType(string contentType) => new(contentType);
    public override string ToString() => m_Charset;
    
    //Custom
    public static readonly HttpCharsetType X_USER_DEFINED = new("x-user-defined");
    
    //ISO 646
    public static readonly HttpCharsetType US_ASCII = new("us-ascii");

    //Unicode
    public static readonly HttpCharsetType UTF_8 = new("utf-8");
    public static readonly HttpCharsetType UTF_16 = new("utf-16");
    public static readonly HttpCharsetType UTF_32 = new("utf-32");

    //MS-Windows
    public static readonly HttpCharsetType WINDOWS_1250 = new("windows-1250");
    public static readonly HttpCharsetType WINDOWS_1251 = new("windows-1251");
    public static readonly HttpCharsetType WINDOWS_1252 = new("windows-1252");
    public static readonly HttpCharsetType WINDOWS_1253 = new("windows-1253");
    public static readonly HttpCharsetType WINDOWS_1254 = new("windows-1254");
    public static readonly HttpCharsetType WINDOWS_1255 = new("windows-1255");
    public static readonly HttpCharsetType WINDOWS_1256 = new("windows-1256");
    public static readonly HttpCharsetType WINDOWS_1257 = new("windows-1257");
    public static readonly HttpCharsetType WINDOWS_1258 = new("windows-1258");
    
    //Apple
    public static readonly HttpCharsetType MACINTOSH = new("macintosh");
    public static readonly HttpCharsetType X_MAC_CYRILLIC = new("x-mac-cyrillic");
    
    //ISO 8859
    public static readonly HttpCharsetType ISO_8859_1 = new("iso-8859-1");
    public static readonly HttpCharsetType ISO_8859_2 = new("iso-8859-2");
    public static readonly HttpCharsetType ISO_8859_3 = new("iso-8859-3");
    public static readonly HttpCharsetType ISO_8859_4 = new("iso-8859-4");
    public static readonly HttpCharsetType ISO_8859_5 = new("iso-8859-5");
    public static readonly HttpCharsetType ISO_8859_6 = new("iso-8859-6");
    public static readonly HttpCharsetType ISO_8859_7 = new("iso-8859-7");
    public static readonly HttpCharsetType ISO_8859_8 = new("iso-8859-8");
    public static readonly HttpCharsetType ISO_8859_9 = new("iso-8859-9");
    public static readonly HttpCharsetType ISO_8859_10 = new("iso-8859-10");
    public static readonly HttpCharsetType ISO_8859_11 = new("iso-8859-11");
    public static readonly HttpCharsetType ISO_8859_13 = new("iso-8859-13");
    public static readonly HttpCharsetType ISO_8859_14 = new("iso-8859-14");
    public static readonly HttpCharsetType ISO_8859_15 = new("iso-8859-15");
    public static readonly HttpCharsetType ISO_8859_16 = new("iso-8859-16");
    
    //KOI
    public static readonly HttpCharsetType KOI7 = new("koi7");
    public static readonly HttpCharsetType KOI8_R = new("koi8-r");
    public static readonly HttpCharsetType KOI8_U = new("koi8-u");

    //EUC
    public static readonly HttpCharsetType EUC_TW = new("euc-tw");
    public static readonly HttpCharsetType EUC_JP = new("euc-jp");
    public static readonly HttpCharsetType EUC_CN = new("euc-cn");
    public static readonly HttpCharsetType EUC_KR = new("euc-kr");
    
    //TIS
    public static readonly HttpCharsetType TIS_620 = new("tis-620");
    
    //ISO-2022
    public static readonly HttpCharsetType ISO_2022_JP = new("iso-2022-jp");
    public static readonly HttpCharsetType ISO_2022_CN = new("iso-2022-cn");
    public static readonly HttpCharsetType ISO_2022_KR = new("iso-2022-kr");

    //Chinese
    public static readonly HttpCharsetType GBK = new("gbk");
    public static readonly HttpCharsetType GB18030 = new("gb18030");
    public static readonly HttpCharsetType GB2312 = new("gb2312");
    
    //Taiwan
    public static readonly HttpCharsetType BIG5 = new("big5");
    
    //Hong Kong
    public static readonly HttpCharsetType HKSCS = new("hkscs");
    
    //JIS X 0208
    public static readonly HttpCharsetType SHIFT_JIS = new("shift_jis");
    
    //Others
    public static readonly HttpCharsetType ISCII = new("iscii");
    public static readonly HttpCharsetType TSCII = new("tscii");
    public static readonly HttpCharsetType VISCII = new("viscii");
}