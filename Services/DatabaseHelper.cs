using SQLite;
using dashboard.Tables;

    namespace dashboard.Services
{
    public class DatabaseHelper
    {
        private readonly SQLiteAsyncConnection _database;

        public DatabaseHelper(string dbPath)
        {
            _database = new SQLiteAsyncConnection(dbPath);
        }

        public async Task InitAsync()
        {
            await _database.DropTableAsync<Modules>(); //herlaad het voor testen
            await _database.CreateTableAsync<Modules>();

            await _database.CreateTableAsync<Eenheid>();

            await _database.CreateTableAsync<Paden>();

            await _database.CreateTableAsync<Eenheden>();
        }

        //CRUD create, read, update and delete
        public Task<List<Modules>> GetModulesAsync()
        {
            return _database.Table<Modules>().ToListAsync();
        }

        public Task<Modules> GetModuleByIdAsync(int id)
        {
            return _database.Table<Modules>().Where(m => m.Id == id).FirstOrDefaultAsync();
        }

        public Task<int> SaveModulesAsync(Modules item)
        {
            if (item.Id != 0)
                return _database.UpdateAsync(item);
            else
                return _database.InsertAsync(item);
        }

        public Task<int> DeleteModulesAsync(Modules item)
        {
            return _database.DeleteAsync(item);
        }

        // Eenheid CRUD
        public Task<List<Eenheid>> GetEenhedenAsync()
        {
            return _database.Table<Eenheid>().ToListAsync();
        }

        public Task<Eenheid> GetEenheidByIdAsync(int id)
        {
            return _database.Table<Eenheid>().Where(e => e.Id == id).FirstOrDefaultAsync();
        }

        public Task<int> SaveEenheidAsync(Eenheid item)
        {
            if (item.Id != 0)
                return _database.UpdateAsync(item);
            else
                return _database.InsertAsync(item);
        }

        public Task<int> DeleteEenheidAsync(Eenheid item)
        {
            return _database.DeleteAsync(item);
        }

        // Paden CRUD
        public Task<List<Paden>> GetPadenAsync()
        {
            return _database.Table<Paden>().ToListAsync();
        }

        public Task<Paden> GetPadenByIdAsync(int id)
        {
            return _database.Table<Paden>().Where(p => p.Id == id).FirstOrDefaultAsync();
        }

        public Task<int> SavePadenAsync(Paden item)
        {
            if (item.Id != 0)
                return _database.UpdateAsync(item);
            else
                return _database.InsertAsync(item);
        }

        public Task<int> DeletePadenAsync(Paden item)
        {
            return _database.DeleteAsync(item);
        }

        // Eenheden CRUD
        public Task<List<Eenheden>> GetAllEenhedenAsync()
        {
            return _database.Table<Eenheden>().ToListAsync();
        }

        public Task<Eenheden> GetEenhedenByIdAsync(int id)
        {
            return _database.Table<Eenheden>().Where(e => e.Id == id).FirstOrDefaultAsync();
        }

        public Task<int> SaveEenhedenAsync(Eenheden item)
        {
            if (item.Id != 0)
                return _database.UpdateAsync(item);
            else
                return _database.InsertAsync(item);
        }

        public Task<int> DeleteEenhedenAsync(Eenheden item)
        {
            return _database.DeleteAsync(item);
        }
    }
}
