using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PastaMaster.Core
{
    static class DatabaseHandler 
    {
        public async static Task<Task> FixPastaIds()
        {
            var all = await MongoMessageRepository.GetAllMessages();
            System.Console.WriteLine($"Database length: {all.Count}");

            var deleted = new List<MessageRecord>();

            for (int i = 0; i < all.Count-1; i++)
            {
                if (deleted.Contains(all[i])) continue;
                var listOneManSpam = new List<MessageRecord>();
                var foundOneMan = true;

                var id = all[i].PastaId;
                var name = all[i].Name;
                listOneManSpam.Add(all[i]);
                for (int j = i+1; j < all.Count; j++)
                {
                    if (RegexUtills.GetLevenshteinDistancePercent(all[j].Message, all[i].Message) >= 80)
                    {
                        if (all[j].PastaId != all[i].PastaId)
                        {
                            if (!await MongoMessageRepository.UpdateMessage(all[j].Id, "pastaId", all[i].PastaId.ToString()))
                            {
                                System.Console.WriteLine("Couldn't update record, consider as error");
                            }   
                        }
                        if (name == all[j].Name && foundOneMan)
                        {
                            listOneManSpam.Add(all[j]);    
                        }
                        else if (name != all[j].Name)
                        {
                            foundOneMan = false;
                        }
                    }
                }

                if (foundOneMan && listOneManSpam.Count > 0)
                {
                    System.Console.WriteLine($"Found one man spam: {listOneManSpam[0].Name}");
                    deleted.AddRange(listOneManSpam);
                    foreach (var item in listOneManSpam)
                    {
                        await MongoMessageRepository.DeleteMessageById(item.Id);
                    }
                }
            }
            
            return Task.CompletedTask;      
        }
    }
}