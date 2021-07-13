namespace API.Helpers
{
    public class UserParams
    {
        public const int maxPageSize = 50;
        public int PageNumber { get; set; } = 1;
        private int _pageSize = 10;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > maxPageSize) ? maxPageSize : value;
        }
        public string CurrentUsername { get; set; }
        public string Gender { get; set; }
        public int MinAge { get; set; } = 18;
        public int Maxge { get; set; } = 150;
        public string OrderBy { get; set; }="lastActive";
    }
}