namespace BeArtGallery.Authorization
{
    // Nomi dei ruoli usati in tutto il progetto
    public static class RoleNames
    {
        public const string Visitore = "Visitore";
        public const string Staff = "Staff";
        public const string Admin = "Admin";

        // Da usare negli attributi [Authorize] per Staff e Admin insieme
        public const string StaffOrAdmin = Staff + "," + Admin;
    }
}