namespace AutoGen
{
    /// <summary>
    /// 根对象
    /// </summary>
    public class Rootobject
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// URL
        /// </summary>
        public string url { get; set; }
        /// <summary>
        /// page信息
        /// </summary>
        public int page { get; set; }
        /// <summary>
        /// 是否包含配置
        /// </summary>
        public bool isNonProfit { get; set; }
        /// <summary>
        /// 地址信息
        /// </summary>
        public Address address { get; set; }
        /// <summary>
        /// link资源
        /// </summary>
        public Link[] links { get; set; }
    }

    /// <summary>
    /// 地址信息
    /// </summary>
    public class Address
    {
        /// <summary>
        /// 街道
        /// </summary>
        public string street { get; set; }
        /// <summary>
        /// 城市
        /// </summary>
        public string city { get; set; }
        /// <summary>
        /// 国家
        /// </summary>
        public string country { get; set; }
    }

    /// <summary>
    /// link对象
    /// </summary>
    public class Link
    {
        /// <summary>
        /// 链接名称
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// 链接实际地址
        /// </summary>
        public string url { get; set; }
    }
}