using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace MongoDB.Client
{
    public class MongoBAIClient
    {
        public async Task ImportDataEntriesInDatabase(string jsonData, string category)
        {
            //var settings = MongoClientSettings.FromConnectionString("mongodb+srv://admin:I36oVbYYcYvl5ROs@ppmcbai.7ynygj2.mongodb.net/?retryWrites=true&w=majority");
            //settings.ServerApi = new ServerApi(ServerApiVersion.V1);
            var client = new MongoClient("mongodb+srv://admin:I36oVbYYcYvl5ROs@ppmcbai.7ynygj2.mongodb.net/?retryWrites=true&w=majority");

            try
            {
                // Send a ping to confirm a successful connection
                await client.GetDatabase("BAI_Test").RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
                Console.WriteLine("Pinged your deployment. You successfully connected to MongoDB!");

                // Set the database and collection based on the category
                var database = client.GetDatabase("BAI_Test"); // Replace with your database name
                var collection = database.GetCollection<BsonDocument>(category);

                // Parse the JSON data
                var document = BsonDocument.Parse(jsonData);

                // Insert the document
                await collection.InsertOneAsync(document);
                Console.WriteLine("Daten wurden erfolgreich in die MongoDB geschrieben.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ein Fehler ist aufgetreten: " + ex.Message);
            }
        }
    }
}
