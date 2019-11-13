using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PastaMaster.Core
{
    static class DatabaseHandler 
    {
        public static async Task<Task> FixPastaIds()
        {
//            var all = await MongoMessageRepository.GetAllMessages();
//            System.Console.WriteLine($"Database length: {all.Count}");
//
//            var deleted = new List<MessageRecord>();
//
//            for (int i = 0; i < all.Count-1; i++)
//            {
//                if (deleted.Contains(all[i])) continue;
//                var listOneManSpam = new List<MessageRecord>();
//                var foundOneMan = true;
//
//                var id = all[i].PastaId;
//                var name = all[i].Name;
//                listOneManSpam.Add(all[i]);
//                for (int j = i+1; j < all.Count; j++)
//                {
//                    if (RegexUtills.GetLevenshteinDistancePercent(all[j].Message, all[i].Message) >= 80)
//                    {
//                        if (all[j].PastaId != all[i].PastaId)
//                        {
//                            if (!await MongoMessageRepository.UpdateMessage(all[j].Id, "pastaId", all[i].PastaId.ToString()))
//                            {
//                                System.Console.WriteLine("Couldn't update record, consider as error");
//                            }   
//                        }
//                        if (name == all[j].Name && foundOneMan)
//                        {
//                            listOneManSpam.Add(all[j]);    
//                        }
//                        else if (name != all[j].Name)
//                        {
//                            foundOneMan = false;
//                        }
//                    }
//                }
//
//                if (foundOneMan && listOneManSpam.Count > 0)
//                {
//                    System.Console.WriteLine($"Found one man spam: {listOneManSpam[0].Name}");
//                    deleted.AddRange(listOneManSpam);
//                    foreach (var item in listOneManSpam)
//                    {
//                        await MongoMessageRepository.DeleteMessageById(item.Id);
//                    }
//                }
//            }


            
            return Task.CompletedTask;      
        }

        public static async Task<int> AddPastaInDatabase()
        {
            var allMessages = await MongoRepository.GetAllMessages();
            var allPastas = await MongoRepository.GetAllPastas();
            System.Console.WriteLine($"Database length: {allMessages.Count}");

            var uniquePastas = new List<PastaRecord>();
            foreach (var messageRecord in from messageRecord in allMessages 
                let found = uniquePastas.Any(uniquePasta => 
                                RegexUtills.GetLevenshteinDistancePercent(
                                    uniquePasta.Message, messageRecord.Message) >= 80) || 
                            allPastas.Any(pastaRecord => 
                                RegexUtills.GetLevenshteinDistancePercent(
                                    pastaRecord.Message, messageRecord.Message) >= 80) where !found select messageRecord)
            {
                uniquePastas.Add(new PastaRecord(messageRecord.Message));
            }

            if (uniquePastas.Count > 0)
                await MongoRepository.InsertPastas(uniquePastas);

            return uniquePastas.Count;
        }
    }
}