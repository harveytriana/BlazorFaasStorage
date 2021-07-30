namespace BlazorFaaSStorage
{
    public static class Extensions
    {
        public static bool Empty(this string text)
        {
            return string.IsNullOrEmpty(text);
        }

        public static string ToFileSize(this long lenght)
        {
            var size = lenght;
            var index = 0;
            for (; size > 1000; index++) {
                size /= 1000;
            }
            return size.ToString("0.000 " + new[] { "b", "kb", "mb", "gb", "tb" }[index]);
        }

        public static bool IsContentImage(this string contentType)
        {
            if (contentType is null) {
                return false;
            }
            return contentType.Contains("jpeg") ||
                   contentType.Contains("png") ||
                   contentType.Contains("jpg") ||
                   contentType.Contains("gif");
        }

        public static string FixContentType(this string contentype)
        {
            return contentype.Empty() ? "application/octet-stream" : contentype;
        }
    }
}
