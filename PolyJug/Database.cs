namespace PolyJug {
    interface Database {

        string connectionString {get; set;}

        bool Open(string connectionString);

        

        
    }
}
