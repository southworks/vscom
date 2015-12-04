namespace VSCOM.ReverseArticles
{
    using System.Collections.Generic;

    public class TOC
    {
        public TOC()
        {
            this.Sections = new List<Section>();
        }

        public List<Section> Sections { get; set; }
    }
}
