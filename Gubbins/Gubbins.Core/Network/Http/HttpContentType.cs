/*
 * Copyright ©2022 Gatongone
 * Author: Gatongone
 * Email: gatongone@gmail.com
 * Created On: 2023/08/12-01:38:15
 * Github: https://github.com/Gatongone
 * Description: Http content type.
 */

namespace Gubbins.Network;

/// <summary>
/// Http content types.
/// </summary>
// ReSharper disable All
public struct HttpContentType
{
    private readonly string m_Content;
    public HttpContentType(string content) => m_Content = content;
    public static implicit operator string(HttpContentType contentType) => contentType.m_Content;
    public static implicit operator HttpContentType(string contentType) => new(contentType);
    public HttpContentType WithCharset(HttpCharsetType charsetType) => new($"{m_Content}; charset={charsetType.ToString()}");
    public HttpContentType WithCharset(string charsetType) => new($"{m_Content}; charset={charsetType}");
    public override string ToString() => m_Content;

    public static class Application
    {
        public static readonly HttpContentType FRACTALS = new("application/fractals");
        public static readonly HttpContentType FUTURESPLASH = new("application/futuresplash");
        public static readonly HttpContentType HTA = new("application/hta");
        public static readonly HttpContentType JSON = new("application/json");
        public static readonly HttpContentType MAC_BINHEX40 = new("application/mac-binhex40");
        public static readonly HttpContentType MSACCESS = new("application/msaccess");
        public static readonly HttpContentType MSWORD = new("application/msword");
        public static readonly HttpContentType OCTET_STREAM = new("application/octet-stream");
        public static readonly HttpContentType PDF = new("application/pdf");
        public static readonly HttpContentType PICS_RULES = new("application/pics-rules");
        public static readonly HttpContentType PKCS10 = new("application/pkcs10");
        public static readonly HttpContentType PKCS7_MIME = new("application/pkcs7-mime");
        public static readonly HttpContentType PKCS7_SIGNATURE = new("application/pkcs7-signature");
        public static readonly HttpContentType PKIX_CRL = new("application/pkix-crl");
        public static readonly HttpContentType POSTSCRIPT = new("application/postscript");
        public static readonly HttpContentType RAT_FILE = new("application/rat-file");
        public static readonly HttpContentType SDP = new("application/sdp");
        public static readonly HttpContentType SMIL = new("application/smil");
        public static readonly HttpContentType STREAMINGMEDIA = new("application/streamingmedia");
        public static readonly HttpContentType VND_ADOBE_EDN = new("application/vnd.adobe.edn");
        public static readonly HttpContentType VND_ADOBE_PDX = new("application/vnd.adobe.pdx");
        public static readonly HttpContentType VND_ADOBE_RMF = new("application/vnd.adobe.rmf");
        public static readonly HttpContentType VND_ADOBE_WORKFLOW = new("application/vnd.adobe.workflow");
        public static readonly HttpContentType VND_ADOBE_XDP = new("application/vnd.adobe.xdp");
        public static readonly HttpContentType VND_ADOBE_XFD = new("application/vnd.adobe.xfd");
        public static readonly HttpContentType VND_ADOBE_XFDF = new("application/vnd.adobe.xfdf");
        public static readonly HttpContentType VND_ANDROID_PACKAGE_ARCHIVE = new("application/vnd.android.package-archive");
        public static readonly HttpContentType VND_FDF = new("application/vnd.fdf");
        public static readonly HttpContentType VND_IPHONE = new("application/vnd.iphone");
        public static readonly HttpContentType VND_MS_EXCEL = new("application/vnd.ms-excel");
        public static readonly HttpContentType VND_MS_PKI_CERTSTORE = new("application/vnd.ms-pki.certstore");
        public static readonly HttpContentType VND_MS_PKI_PKO = new("application/vnd.ms-pki.pko");
        public static readonly HttpContentType VND_MS_PKI_SECCAT = new("application/vnd.ms-pki.seccat");
        public static readonly HttpContentType VND_MS_PKI_STL = new("application/vnd.ms-pki.stl");
        public static readonly HttpContentType VND_MS_POWERPOINT = new("application/vnd.ms-powerpoint");
        public static readonly HttpContentType VND_MS_PROJECT = new("application/vnd.ms-project");
        public static readonly HttpContentType VND_MS_WPL = new("application/vnd.ms-wpl");
        public static readonly HttpContentType VND_RN_REALMEDIA = new("application/vnd.rn-realmedia");
        public static readonly HttpContentType VND_RN_REALMEDIA_SECURE = new("application/vnd.rn-realmedia-secure");
        public static readonly HttpContentType VND_RN_REALMEDIA_VBR = new("application/vnd.rn-realmedia-vbr");
        public static readonly HttpContentType VND_RN_REALPLAYER = new("application/vnd.rn-realplayer");
        public static readonly HttpContentType VND_RN_REALSYSTEM_RJS = new("application/vnd.rn-realsystem-rjs");
        public static readonly HttpContentType VND_RN_REALSYSTEM_RJT = new("application/vnd.rn-realsystem-rjt");
        public static readonly HttpContentType VND_RN_REALSYSTEM_RMJ = new("application/vnd.rn-realsystem-rmj");
        public static readonly HttpContentType VND_RN_REALSYSTEM_RMX = new("application/vnd.rn-realsystem-rmx");
        public static readonly HttpContentType VND_RN_RECORDING = new("application/vnd.rn-recording");
        public static readonly HttpContentType VND_RN_RN_MUSIC_PACKAGE = new("application/vnd.rn-rn_music_package");
        public static readonly HttpContentType VND_RN_RSML = new("application/vnd.rn-rsml");
        public static readonly HttpContentType VND_SYMBIAN_INSTALL = new("application/vnd.symbian.install");
        public static readonly HttpContentType VND_VISIO = new("application/vnd.visio");
        public static readonly HttpContentType X_ = new("application/x-");
        public static readonly HttpContentType X_001 = new("application/x-001");
        public static readonly HttpContentType X_301 = new("application/x-301");
        public static readonly HttpContentType X_906 = new("application/x-906");
        public static readonly HttpContentType X_A11 = new("application/x-a11");
        public static readonly HttpContentType X_ANV = new("application/x-anv");
        public static readonly HttpContentType X_BITTORRENT = new("application/x-bittorrent");
        public static readonly HttpContentType X_BMP = new("application/x-bmp");
        public static readonly HttpContentType X_BOT = new("application/x-bot");
        public static readonly HttpContentType X_C4T = new("application/x-c4t");
        public static readonly HttpContentType X_C90 = new("application/x-c90");
        public static readonly HttpContentType X_CALS = new("application/x-cals");
        public static readonly HttpContentType X_CDR = new("application/x-cdr");
        public static readonly HttpContentType X_CEL = new("application/x-cel");
        public static readonly HttpContentType X_CGM = new("application/x-cgm");
        public static readonly HttpContentType X_CIT = new("application/x-cit");
        public static readonly HttpContentType X_CMP = new("application/x-cmp");
        public static readonly HttpContentType X_CMX = new("application/x-cmx");
        public static readonly HttpContentType X_COT = new("application/x-cot");
        public static readonly HttpContentType X_CSI = new("application/x-csi");
        public static readonly HttpContentType X_CUT = new("application/x-cut");
        public static readonly HttpContentType X_DBF = new("application/x-dbf");
        public static readonly HttpContentType X_DBM = new("application/x-dbm");
        public static readonly HttpContentType X_DBX = new("application/x-dbx");
        public static readonly HttpContentType X_DCX = new("application/x-dcx");
        public static readonly HttpContentType X_DGN = new("application/x-dgn");
        public static readonly HttpContentType X_DIB = new("application/x-dib");
        public static readonly HttpContentType X_DRW = new("application/x-drw");
        public static readonly HttpContentType X_DWF = new("application/x-dwf");
        public static readonly HttpContentType X_DWG = new("application/x-dwg");
        public static readonly HttpContentType X_DXB = new("application/x-dxb");
        public static readonly HttpContentType X_DXF = new("application/x-dxf");
        public static readonly HttpContentType X_EBX = new("application/x-ebx");
        public static readonly HttpContentType X_EMF = new("application/x-emf");
        public static readonly HttpContentType X_EPI = new("application/x-epi");
        public static readonly HttpContentType X_FRM = new("application/x-frm");
        public static readonly HttpContentType X_G4 = new("application/x-g4");
        public static readonly HttpContentType X_GBR = new("application/x-gbr");
        public static readonly HttpContentType X_GL2 = new("application/x-gl2");
        public static readonly HttpContentType X_GP4 = new("application/x-gp4");
        public static readonly HttpContentType X_HGL = new("application/x-hgl");
        public static readonly HttpContentType X_HMR = new("application/x-hmr");
        public static readonly HttpContentType X_HPGL = new("application/x-hpgl");
        public static readonly HttpContentType X_HPL = new("application/x-hpl");
        public static readonly HttpContentType X_HRF = new("application/x-hrf");
        public static readonly HttpContentType X_ICB = new("application/x-icb");
        public static readonly HttpContentType X_ICO = new("application/x-ico");
        public static readonly HttpContentType X_ICQ = new("application/x-icq");
        public static readonly HttpContentType X_IFF = new("application/x-iff");
        public static readonly HttpContentType X_IGS = new("application/x-igs");
        public static readonly HttpContentType X_IMG = new("application/x-img");
        public static readonly HttpContentType X_INTERNET_SIGNUP = new("application/x-internet-signup");
        public static readonly HttpContentType X_IPHONE = new("application/x-iphone");
        public static readonly HttpContentType X_JAVASCRIPT = new("application/x-javascript");
        public static readonly HttpContentType X_JPE = new("application/x-jpe");
        public static readonly HttpContentType X_JPG = new("application/x-jpg");
        public static readonly HttpContentType X_LAPLAYER_REG = new("application/x-laplayer-reg");
        public static readonly HttpContentType X_LATEX = new("application/x-latex");
        public static readonly HttpContentType X_LBM = new("application/x-lbm");
        public static readonly HttpContentType X_LTR = new("application/x-ltr");
        public static readonly HttpContentType X_MAC = new("application/x-mac");
        public static readonly HttpContentType X_MDB = new("application/x-mdb");
        public static readonly HttpContentType X_MI = new("application/x-mi");
        public static readonly HttpContentType X_MIL = new("application/x-mil");
        public static readonly HttpContentType X_MMXP = new("application/x-mmxp");
        public static readonly HttpContentType X_MS_WMD = new("application/x-ms-wmd");
        public static readonly HttpContentType X_MS_WMZ = new("application/x-ms-wmz");
        public static readonly HttpContentType X_MSDOWNLOAD = new("application/x-msdownload");
        public static readonly HttpContentType X_NETCDF = new("application/x-netcdf");
        public static readonly HttpContentType X_NRF = new("application/x-nrf");
        public static readonly HttpContentType X_OUT = new("application/x-out");
        public static readonly HttpContentType X_PC5 = new("application/x-pc5");
        public static readonly HttpContentType X_PCI = new("application/x-pci");
        public static readonly HttpContentType X_PCL = new("application/x-pcl");
        public static readonly HttpContentType X_PCX = new("application/x-pcx");
        public static readonly HttpContentType X_PERL = new("application/x-perl");
        public static readonly HttpContentType X_PGL = new("application/x-pgl");
        public static readonly HttpContentType X_PIC = new("application/x-pic");
        public static readonly HttpContentType X_PKCS12 = new("application/x-pkcs12");
        public static readonly HttpContentType X_PKCS7_CERTIFICATES = new("application/x-pkcs7-certificates");
        public static readonly HttpContentType X_PKCS7_CERTREQRESP = new("application/x-pkcs7-certreqresp");
        public static readonly HttpContentType X_PLT = new("application/x-plt");
        public static readonly HttpContentType X_PNG = new("application/x-png");
        public static readonly HttpContentType X_PPM = new("application/x-ppm");
        public static readonly HttpContentType X_PPT = new("application/x-ppt");
        public static readonly HttpContentType X_PR = new("application/x-pr");
        public static readonly HttpContentType X_PRN = new("application/x-prn");
        public static readonly HttpContentType X_PRT = new("application/x-prt");
        public static readonly HttpContentType X_PS = new("application/x-ps");
        public static readonly HttpContentType X_PTN = new("application/x-ptn");
        public static readonly HttpContentType X_RAS = new("application/x-ras");
        public static readonly HttpContentType X_RED = new("application/x-red");
        public static readonly HttpContentType X_RGB = new("application/x-rgb");
        public static readonly HttpContentType X_RLC = new("application/x-rlc");
        public static readonly HttpContentType X_RLE = new("application/x-rle");
        public static readonly HttpContentType X_RTF = new("application/x-rtf");
        public static readonly HttpContentType X_SAM = new("application/x-sam");
        public static readonly HttpContentType X_SAT = new("application/x-sat");
        public static readonly HttpContentType X_SDW = new("application/x-sdw");
        public static readonly HttpContentType X_SHOCKWAVE_FLASH = new("application/x-shockwave-flash");
        public static readonly HttpContentType X_SILVERLIGHT_APP = new("application/x-silverlight-app");
        public static readonly HttpContentType X_SLB = new("application/x-slb");
        public static readonly HttpContentType X_SLD = new("application/x-sld");
        public static readonly HttpContentType X_SMK = new("application/x-smk");
        public static readonly HttpContentType X_STUFFIT = new("application/x-stuffit");
        public static readonly HttpContentType X_STY = new("application/x-sty");
        public static readonly HttpContentType X_TDF = new("application/x-tdf");
        public static readonly HttpContentType X_TG4 = new("application/x-tg4");
        public static readonly HttpContentType X_TGA = new("application/x-tga");
        public static readonly HttpContentType X_TIF = new("application/x-tif");
        public static readonly HttpContentType X_TROFF_MAN = new("application/x-troff-man");
        public static readonly HttpContentType X_VDA = new("application/x-vda");
        public static readonly HttpContentType X_VPEG005 = new("application/x-vpeg005");
        public static readonly HttpContentType X_VSD = new("application/x-vsd");
        public static readonly HttpContentType X_VST = new("application/x-vst");
        public static readonly HttpContentType X_WB1 = new("application/x-wb1");
        public static readonly HttpContentType X_WB2 = new("application/x-wb2");
        public static readonly HttpContentType X_WB3 = new("application/x-wb3");
        public static readonly HttpContentType X_WK3 = new("application/x-wk3");
        public static readonly HttpContentType X_WK4 = new("application/x-wk4");
        public static readonly HttpContentType X_WKQ = new("application/x-wkq");
        public static readonly HttpContentType X_WKS = new("application/x-wks");
        public static readonly HttpContentType X_WMF = new("application/x-wmf");
        public static readonly HttpContentType X_WP6 = new("application/x-wp6");
        public static readonly HttpContentType X_WPD = new("application/x-wpd");
        public static readonly HttpContentType X_WPG = new("application/x-wpg");
        public static readonly HttpContentType X_WQ1 = new("application/x-wq1");
        public static readonly HttpContentType X_WR1 = new("application/x-wr1");
        public static readonly HttpContentType X_WRI = new("application/x-wri");
        public static readonly HttpContentType X_WRK = new("application/x-wrk");
        public static readonly HttpContentType X_WS = new("application/x-ws");
        public static readonly HttpContentType X_X_B = new("application/x-x_b");
        public static readonly HttpContentType X_X_T = new("application/x-x_t");
        public static readonly HttpContentType X_X509_CA_CERT = new("application/x-x509-ca-cert");
        public static readonly HttpContentType X_XLS = new("application/x-xls");
        public static readonly HttpContentType X_XLW = new("application/x-xlw");
        public static readonly HttpContentType X_XWD = new("application/x-xwd");
    }

    public static class Audio
    {
        public static readonly HttpContentType AIFF = new("audio/aiff");
        public static readonly HttpContentType BASIC = new("audio/basic");
        public static readonly HttpContentType MID = new("audio/mid");
        public static readonly HttpContentType MP1 = new("audio/mp1");
        public static readonly HttpContentType MP2 = new("audio/mp2");
        public static readonly HttpContentType MP3 = new("audio/mp3");
        public static readonly HttpContentType MPEGURL = new("audio/mpegurl");
        public static readonly HttpContentType RN_MPEG = new("audio/rn-mpeg");
        public static readonly HttpContentType SCPLS = new("audio/scpls");
        public static readonly HttpContentType VND_RN_REALAUDIO = new("audio/vnd.rn-realaudio");
        public static readonly HttpContentType WAV = new("audio/wav");
        public static readonly HttpContentType X_LA_LMS = new("audio/x-la-lms");
        public static readonly HttpContentType X_LIQUID_FILE = new("audio/x-liquid-file");
        public static readonly HttpContentType X_LIQUID_SECURE = new("audio/x-liquid-secure");
        public static readonly HttpContentType X_MEI_AAC = new("audio/x-mei-aac");
        public static readonly HttpContentType X_MS_WAX = new("audio/x-ms-wax");
        public static readonly HttpContentType X_MS_WMA = new("audio/x-ms-wma");
        public static readonly HttpContentType X_MUSICNET_DOWNLOAD = new("audio/x-musicnet-download");
        public static readonly HttpContentType X_MUSICNET_STREAM = new("audio/x-musicnet-stream");
        public static readonly HttpContentType X_PN_REALAUDIO = new("audio/x-pn-realaudio");
        public static readonly HttpContentType X_PN_REALAUDIO_PLUGIN = new("audio/x-pn-realaudio-plugin");
    }

    public static class Drawing
    {
        public static readonly HttpContentType _907 = new ("drawing/907");
        public static readonly HttpContentType X_SLK = new("drawing/x-slk");
        public static readonly HttpContentType X_TOP = new("drawing/x-top");
    }

    public static class Image
    {
        public static readonly HttpContentType FAX = new("image/fax");
        public static readonly HttpContentType GIF = new("image/gif");
        public static readonly HttpContentType JPEG = new("image/jpeg");
        public static readonly HttpContentType PNETVUE = new("image/pnetvue");
        public static readonly HttpContentType PNG = new("image/png");
        public static readonly HttpContentType TIFF = new("image/tiff");
        public static readonly HttpContentType VND_RN_REALPIX = new("image/vnd.rn-realpix");
        public static readonly HttpContentType VND_WAP_WBMP = new("image/vnd.wap.wbmp");
        public static readonly HttpContentType X_ICON = new("image/x-icon");
    }

    public static class Java
    {
        public static readonly HttpContentType ANY = new ("java/*");
    }

    public static class Message
    {
        public static readonly HttpContentType RFC822 = new("message/rfc822");
    }

    public static class Model
    {
        public static readonly HttpContentType VND_DWF = new("Model/vnd.dwf");
    }

    public static class Text
    {
        public static readonly HttpContentType ASA = new("text/asa");
        public static readonly HttpContentType ASP = new("text/asp");
        public static readonly HttpContentType CSS = new("text/css");
        public static readonly HttpContentType H323 = new("text/h323");
        public static readonly HttpContentType HTML = new("text/html");
        public static readonly HttpContentType IULS = new("text/iuls");
        public static readonly HttpContentType PLAIN = new("text/plain");
        public static readonly HttpContentType SCRIPTLET = new("text/scriptlet");
        public static readonly HttpContentType VND_RN_REALTEXT = new("text/vnd.rn-realtext");
        public static readonly HttpContentType VND_RN_REALTEXT3D = new("text/vnd.rn-realtext3d");
        public static readonly HttpContentType VND_WAP_WML = new("text/vnd.wap.wml");
        public static readonly HttpContentType WEBVIEWHTML = new("text/webviewhtml");
        public static readonly HttpContentType X_COMPONENT = new("text/x-component");
        public static readonly HttpContentType X_MS_ODC = new("text/x-ms-odc");
        public static readonly HttpContentType X_VCARD = new("text/x-vcard");
        public static readonly HttpContentType XML = new("text/xml");
    }

    public static class Video
    {
        public static readonly HttpContentType AVI = new("video/avi");
        public static readonly HttpContentType MPEG = new("video/mpeg");
        public static readonly HttpContentType MPEG4 = new("video/mpeg4");
        public static readonly HttpContentType MPG = new("video/mpg");
        public static readonly HttpContentType VND_RN_REALVIDEO = new("video/vnd.rn-realvideo");
        public static readonly HttpContentType X_IVF = new("video/x-ivf");
        public static readonly HttpContentType X_MPEG = new("video/x-mpeg");
        public static readonly HttpContentType X_MPG = new("video/x-mpg");
        public static readonly HttpContentType X_MS_ASF = new("video/x-ms-asf");
        public static readonly HttpContentType X_MS_WM = new("video/x-ms-wm");
        public static readonly HttpContentType X_MS_WMV = new("video/x-ms-wmv");
        public static readonly HttpContentType X_MS_WMX = new("video/x-ms-wmx");
        public static readonly HttpContentType X_MS_WVX = new("video/x-ms-wvx");
        public static readonly HttpContentType X_SGI_MOVIE = new("video/x-sgi-movie");
    }
}