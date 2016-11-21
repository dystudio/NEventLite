﻿using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NEventLite.Command_Bus;
using NEventLite.Command_Handlers;
using NEventLite.Extensions;
using NEventLite.Logger;
using NEventLite.Repository;
using NEventLite.Storage;
using NEventLite_Example.Commands;
using NEventLite_Example.Command_Handlers;
using NEventLite_Example.Domain;
using NEventLite_Example.Read_Model;
using NEventLite_Example.Repository;
using NEventLite_Example.Util;

namespace NEventLite_Example
{
    class Program
    {

        static void Main(string[] args)
        {
            //This path is used to save in memory storage
            string inMemoryStoragePath = AppDomain.CurrentDomain.BaseDirectory + @"App_Data\";

            //Load dependency resolver
            using (var container = new DependencyResolver(inMemoryStoragePath))
            {
                //Set logger
                LogManager.AddLogger(container.Resolve<ILogger>());

                using (new MyLifeTimeScope())
                {
                    DoMockRunAsync(container).Wait();
                }
            }

            Console.WriteLine("Press enter key to exit. \n");
            Console.Read();
        }

        private static async Task DoMockRunAsync(DependencyResolver container)
        {
            //GetAsync ioc container to create our repository
            NoteRepository rep = container.Resolve<NoteRepository>();
            var commandBus = container.Resolve<ICommandBus>();

            Guid savedItemId = Guid.Empty;

            //Try to load a given guid.
            LogManager.Log("Enter a GUID to try to load or leave blank and press enter:", LogSeverity.Warning);
            string strGuid = Console.ReadLine();


            if ((string.IsNullOrEmpty(strGuid) == false) && (Guid.TryParse(strGuid, out savedItemId)))
            {
                Note tmpNote = await rep.GetByIdAsync<Note>(savedItemId);

                if (tmpNote == null)
                {
                    LogManager.Log($"No Note found with Guid: {savedItemId}.", LogSeverity.Critical);
                    return;
                }
            }
            else
            {
                //Create new note
                Guid newItemId = Guid.NewGuid();

                (await commandBus.ExecuteAsync(
                    new CreateNoteCommand(Guid.NewGuid(), newItemId, -1, "Test Note", "Event Sourcing System Demo", "Event Sourcing")))
                    .EnsureSuccess();

                Note tmpNote = await rep.GetByIdAsync<Note>(newItemId);

                LogManager.Log("After Creation: This is version 0 of the AggregateRoot.", LogSeverity.Information);
                LogManager.Log(Newtonsoft.Json.JsonConvert.SerializeObject(tmpNote) + "\n", LogSeverity.Debug);

                savedItemId = newItemId;
            }

            LogManager.Log("Doing some changes now... \n", LogSeverity.Debug);

            //Reload and do some changes
            int lastVersion = (await rep.GetByIdAsync<Note>(savedItemId)).CurrentVersion;

            //Do 5 events cycle to check snapshots too.
            for (int i = 1; i <= 5; i++)
            {
                LogManager.Log($"Applying Changes For Cycle {i}", LogSeverity.Debug);

                var result = (await commandBus.ExecuteAsync(
                                new EditNoteCommand(
                                    Guid.NewGuid(), savedItemId, lastVersion,
                                    $"Test Note 123 Event ({lastVersion + 1})",
                                    $"Event Sourcing System Demo ({lastVersion + 2})",
                                    $"Event Sourcing in .NET Example. Event ({lastVersion + 3})")))
                                    .EnsureSuccess();

                lastVersion = result.AggregateVersion;
            }

            LogManager.Log("Finished applying changes. \n", LogSeverity.Debug);

            //Load to display
            Note noteToLoad = await rep.GetByIdAsync<Note>(savedItemId);

            LogManager.Log("\n", LogSeverity.Debug);
            LogManager.Log("This is the write model constructed from event storage:", LogSeverity.Information);
            LogManager.Log(Newtonsoft.Json.JsonConvert.SerializeObject(noteToLoad) + "\n", LogSeverity.Debug);

            //Read model storage
            LogManager.Log("This is the read model loaded from read model storage:", LogSeverity.Information);
            DumpReadModelStorageToScreen(container.Resolve<MyInMemoryReadModelStorage>());

        }

        private static void DumpReadModelStorageToScreen(MyInMemoryReadModelStorage storage)
        {
            foreach (var o in storage.GetAll())
            {
                LogManager.Log(Newtonsoft.Json.JsonConvert.SerializeObject(o) + "\n", LogSeverity.Debug);
            }
        }
    }
}
