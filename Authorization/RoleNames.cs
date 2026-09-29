namespace BeArtGallery.Authorization
{
    public static class RoleNames
    {
        public const string Visitore = "Visitore";
        public const string Staff = "Staff";
        public const string Admin = "Admin";

        public const string StaffOrAdmin = Staff + "," + Admin;
    }
}