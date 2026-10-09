
using System.Collections.Generic;
using System.Linq;

public class CarRepository
{
    private readonly DatabaseManager database;

    public CarRepository(DatabaseManager database)
    {
        this.database = database;
    }

    // Recupera tutte le auto, ordinate per nome.
    public List<CarRecord> GetAllCars()
    {
        return database.Connection
            .Table<CarRecord>()
            .OrderBy(car => car.Name)
            .ToList();
    }

    // Recupera un'auto tramite il suo ID.
    public CarRecord GetCarById(int id)
    {
        return database.Connection
            .Table<CarRecord>()
            .FirstOrDefault(car => car.Id == id);
    }

    // Recupera un'auto tramite il suo nome.
    public CarRecord GetCarByName(string name)
    {
        return database.Connection
            .Table<CarRecord>()
            .FirstOrDefault(car => car.Name == name);
    }

    // Inserisce una nuova auto.
    public int AddCar(CarRecord car)
    {
        return database.Connection.Insert(car);
    }

    // Aggiorna un'auto esistente.
    public int UpdateCar(CarRecord car)
    {
        return database.Connection.Update(car);
    }

    // Elimina un'auto esistente.
    public int DeleteCar(CarRecord car)
    {
        return database.Connection.Delete(car);
    }
}