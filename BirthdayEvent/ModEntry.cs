using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.BellsAndWhistles;

namespace OrbitalEventCreator
{
    public class ModEntry : Mod
    {
        IModHelper helper;

        // The position the NPC with the shop will have
        Vector2 npcPosition = new Vector2(28, 67);

        // The NPC
        StardewValley.NPC newNpc = null;

        // A dictionary which contains all the items
        Dictionary<ISalable, int[]> items = new Dictionary<ISalable, int[]>();

        // The actual shop
        StardewValley.Menus.ShopMenu shop;

        // Whether the birthday has already been started
        Boolean alreadyStarted = false;

        // If the B button has been pressed
        Boolean bPressed = false;

        // Used to spawn fireflies every 30 minutes
        int every30Minutes = 0;

        // The positions of the fairy roses
        Vector2[] rosesPositions = new Vector2[26];
        bool[] rosesFound = new bool[26];
        bool allRosesFound = false;

        // Message class for multiplayer functionality
        private class MyMessage
        {

        }

        //Entry point of the mod
        public override void Entry(IModHelper helper)
        {
            this.helper = helper;
            this.helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            this.helper.Events.Multiplayer.ModMessageReceived += this.OnModMessageReceived;
        }

        //Listen for the message from host player
        private void OnModMessageReceived(object sender, ModMessageReceivedEventArgs e)
        {
           if (e.FromModID == this.ModManifest.UniqueID && e.Type == "StartBirthdayEvent")
           {
                // In case they already have the birthday email, remove it
                if (Game1.player.mailReceived.Contains("WizardBirthdayMail"))
                {
                    Game1.player.mailReceived.Remove("WizardBirthdayMail");
                }

                //Create the letter
                Helper.Content.AssetEditors.Add(new BirthdayLetter());

                //Send the letter for tomorrow
                Game1.addMailForTomorrow("WizardBirthdayMail");

                //This will start the birthday in the next morning
                this.helper.Events.GameLoop.DayStarted += this.OnDayStarted;

                //Set the weather for tomorrow to sunny :D
                Game1.weatherForTomorrow = 4;

                this.Monitor.Log("Received orbital event message.", LogLevel.Debug);
           }
        }

        //Used to start the birthday
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {

            // Ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;

            // If the B button is pressed (by the host)
            if (!bPressed && e.Button == SButton.B && Game1.player.IsMainPlayer)
            {
                this.Monitor.Log("Scheduled a celebration for tomorrow!", LogLevel.Info);
                Game1.addHUDMessage(new HUDMessage("Mail delivered!", 1));

                bPressed = true;

                // Send a message to the farmhand
                MyMessage message = new MyMessage();
                this.Helper.Multiplayer.SendMessage(message, "StartBirthdayEvent", modIDs: new[] { this.ModManifest.UniqueID });

                /*
                // In case they already have the birthday email, remove it
                if (Game1.player.mailReceived.Contains("WizardBirthdayMail"))
                {
                    Game1.player.mailReceived.Remove("WizardBirthdayMail");
                }

                //Create the letter
                Helper.Content.AssetEditors.Add(new BirthdayLetter());

                //Send the letter for tomorrow
                Game1.addMailForTomorrow("WizardBirthdayMail");
                */

                //This will start the birthday in the next morning
                this.helper.Events.GameLoop.DayStarted += this.OnDayStarted;

                //Set the weather for tomorrow to sunny :D
                Game1.weatherForTomorrow = 4;
            }


            // On mouse click
            if (alreadyStarted && Game1.player.currentLocation.Name.Equals("Town") && (e.Button.IsActionButton() || e.Button.IsUseToolButton()) && Game1.overlayMenu == null && !Game1.player.IsMainPlayer)
            {

                ICursorPosition cursorPos = this.Helper.Input.GetCursorPosition();
                Vector2 pos = cursorPos.GrabTile;

                // Check for shop
                // This is called when right clicking on Robin-Wizard
                if (pos == npcPosition && e.Button.IsActionButton())
                {
                    if (allRosesFound)
                    {
                        // Unhook music track change
                        helper.Events.Player.Warped -= OnPlayerWarped;

                        this.Monitor.Log("Click on Wizard", LogLevel.Info);

                        // Activate shop
                        shop = new StardewValley.Menus.ShopMenu(items, 0, null);

                        Game1.activeClickableMenu = shop;

                        //https://github.com/AdamMcIntosh/StawdewValley/blob/master/Menus/ShopMenu.cs
                    }
                    else
                    {
                        // Send a message
                        string message = "You have to find all 26 flowers before you can talk to the Wizard!";
                        Game1.activeClickableMenu = new StardewValley.Menus.DialogueBox(message);
                    }
                }

                // Check to pick roses
                if (!allRosesFound)
                {
                    // Check if a rose has been found
                    for (int i = 0; i < 26; i++)
                    {
                        if (pos == rosesPositions[i])
                        {
                            rosesFound[i] = true;
                            this.Monitor.Log($"Summer spangle {i} picked", LogLevel.Info);

                            // Spawn some butterflies
                            Random rnd = new Random();

                            for (int j = 0; j < 7; j++)
                            {
                                Butterfly b = new Butterfly(new Vector2((int)pos.X, (int)pos.Y));

                                Game1.getLocationFromName("Town").addCritter(b);
                            }
                        }
                    }

                    // Set to true when all the roses have been found
                    allRosesFound = CheckAllRosesPicked();

                    // Send a message
                    if (allRosesFound)
                    {
                        string message = "You have found all the flowers! ^Go talk to the Wizard!";
                        Game1.activeClickableMenu = new StardewValley.Menus.DialogueBox(message);
                    }
                }
            }
        }

        // Called when the day ends
        private void CleanupBirthday(object sender, DayEndingEventArgs e)
        {
            // Delete the merchant
            Game1.removeThisCharacterFromAllLocations(newNpc);

            // Unhook the clean call
            this.helper.Events.GameLoop.DayEnding -= this.CleanupBirthday;

            // Unhook the button event
            this.helper.Events.Input.ButtonPressed -= this.OnButtonPressed;

            //Unhook the timed event
            this.helper.Events.GameLoop.TimeChanged -= this.OnTimeChanged;
        }

        // Check if all the roses have been found
        private bool CheckAllRosesPicked()
        {
            for (int i = 0; i < 26; i++)
            {
                if (rosesFound[i] == false)
                {
                    return false;
                }
            }

            return true;
        }

        //The day of the birthday
        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            // The actual day of the birthday
            this.Monitor.Log("Enabled", LogLevel.Info);

            // Hook the on time changed event to our function
            this.helper.Events.GameLoop.TimeChanged += this.OnTimeChanged;

            // Create the items for the shop
            this.CreateShop();

            // Unhook the day started call
            this.helper.Events.GameLoop.DayStarted -= this.OnDayStarted;

            // Hook cleanup
            this.helper.Events.GameLoop.DayEnding += this.CleanupBirthday;
        }

        //Only active on the birthday day, send a message at 9:00
        private void OnTimeChanged(object sender, TimeChangedEventArgs e)
        {
            // Message at 9:00
            if (e.NewTime == 900)
            {
                //Send a message to all
                Game1.addHUDMessage(new HUDMessage("Birthday celebration has started in town!", 1));

                // Flag to check if the birthday has been started
                this.alreadyStarted = true;
            }

            // Birthday at 9:10
            if (e.NewTime == 910)
            {
                this.StartBirthdayEvent();
            }

            // Add fireflies around the NPC every 30 minutes
            if (alreadyStarted && every30Minutes % 3 == 0)
            {
                Random rnd = new Random();

                for (int i = 0; i < 50; i++)
                {
                    Firefly f = new Firefly(new Vector2(rnd.Next((int)(npcPosition.X - 15), (int)(npcPosition.X + 15)), rnd.Next((int)(npcPosition.Y - 15), (int)(npcPosition.Y + 15))));

                    Game1.getLocationFromName("Town").addCritter(f);
                }
            }

            every30Minutes += 1;

            //Debug
            GameLocation location = Game1.currentLocation;
            int playerX = (int)Math.Floor(Game1.player.Position.X / Game1.tileSize);
            int playerY = (int)Math.Floor(Game1.player.Position.Y / Game1.tileSize);
            //this.Monitor.Log($"Player at ({playerX}, {playerY}) name {location.Name}", LogLevel.Info);
        }

        // Adds the items to the shop
        private void CreateShop()
        {
            StardewValley.Object itm;
            StardewValley.Tools.MeleeWeapon wpn;
            StardewValley.Objects.Hat hat;
            int[] q = new int[2];
            q[0] = 1; //Price 
            q[1] = 1; //Quantity

            // Add party hat
            hat = new StardewValley.Objects.Hat(58);
            items.Add(hat, new int[2] { 0, 1 });
            
            // Add chocolate cake
            itm = new StardewValley.Object(220, 1, quality:4);
            items.Add(itm, new int[2] { 0, 1 });
            
            // Add 3 magic rock candy
            itm = new StardewValley.Object(279, 3);
            items.Add(itm, new int[2] { 0, 3 });
            
            // Add 200 explosive ammo
            itm = new StardewValley.Object(441, 200);
            items.Add(itm, new int[2] { 0, 200 });

            // Add tempered galaxy sword
            wpn = new StardewValley.Tools.MeleeWeapon(66);
            items.Add(wpn, new int[2] { 0, 1 });

        }

        private void StartBirthdayEvent()
        {
            //Here is were I am supposed to do all the cool stuff, is birthday day and we are in town
            this.Monitor.Log("Event started!", LogLevel.Info);

            StardewValley.NPC protoNPC = null;

            //Spawn a custom merchant with Robin-Wizard's sprite
            foreach (StardewValley.GameLocation loc in StardewValley.Game1.locations)
            {
                foreach (StardewValley.NPC npc in loc.characters)
                {
                    if (npc.getName() == "Wizard")
                    {
                        protoNPC = npc;
                    }
                }
            }

            newNpc = new StardewValley.NPC(protoNPC.Sprite, npcPosition, "Town", 2, "Wizard", new Dictionary<int, int[]>(), protoNPC.Portrait, false);
            newNpc.setTileLocation(npcPosition);
            newNpc.Speed = protoNPC.Speed;

            // Spawn Robin-Wizard in town
            Game1.getLocationFromName("Town").addCharacter(newNpc);

            // Try to stop Robin-Wizard
            newNpc.Halt();
            newNpc.stopWithoutChangingFrame();
            newNpc.movementPause = 1000000000;

            //Opens a dialogue as soon as the village is entered
            newNpc.setNewDialogue("We are ready to begin, @! I have hidden 26 summer spangles around town. Find them all, then come talk to me in the middle of the plaza for a reward.");

            if (!Game1.player.IsMainPlayer)
            {
                // Show dialogue
                Game1.drawDialogue(newNpc);
            }
            this.Monitor.Log("NPC spawned!", LogLevel.Info);

            // Change music, if we are currently on town change directly, else set
            // up a hook to change the music when the player enters town
            if (Game1.player.currentLocation.Name.Equals("Town"))
            {
                Game1.changeMusicTrack("WizardSong");
            }
            else
            {
                helper.Events.Player.Warped += OnPlayerWarped;
            }

            // Roses positions
            // Entryway
            rosesPositions[0] = new Vector2(15, 52);
            rosesPositions[1] = new Vector2(6, 52);
            rosesPositions[2] = new Vector2(11, 58);
            rosesPositions[3] = new Vector2(25, 49);
            // Community Center
            rosesPositions[4] = new Vector2(7, 25);
            rosesPositions[5] = new Vector2(34, 18);
            rosesPositions[6] = new Vector2(48, 21);
            rosesPositions[7] = new Vector2(61, 17);
            rosesPositions[8] = new Vector2(70, 20);
            rosesPositions[9] = new Vector2(82, 17);
            // Top right
            rosesPositions[10] = new Vector2(94, 61);
            rosesPositions[11] = new Vector2(106, 20);
            rosesPositions[12] = new Vector2(115, 22);
            rosesPositions[13] = new Vector2(114, 33);
            // Bottom right
            rosesPositions[14] = new Vector2(113, 70);
            rosesPositions[15] = new Vector2(105, 75);
            rosesPositions[16] = new Vector2(107, 93);
            rosesPositions[17] = new Vector2(110, 100);
            // Bottom
            rosesPositions[18] = new Vector2(69, 100);
            rosesPositions[19] = new Vector2(61, 98);
            rosesPositions[20] = new Vector2(45, 102);
            rosesPositions[21] = new Vector2(42, 87);
            rosesPositions[22] = new Vector2(48, 77);
            // Bottom left
            rosesPositions[23] = new Vector2(22, 99);
            rosesPositions[24] = new Vector2(6, 84);
            rosesPositions[25] = new Vector2(26, 77);


            // Set up the color values for summer spangles
            Color[] colorChoice = new Color[]
            {
                new Color(0, 208, 255),
                new Color(99, 255, 210),
                new Color(255, 212, 0),
                new Color(255, 144, 122),
                new Color(255, 0, 238),
                new Color(206, 91, 255),
            };

            // Create randomizer
            Random randColor = new Random();
            int[] tints = new int[26];
            for (int i = 0; i < 26; i++)
            {
                tints[i] = randColor.Next(0, 6);
            }

            // Set the vector to check if they have been picked up and spawn them
            for (int i = 0; i < 26; i++)
            {
                rosesFound[i] = false;

                // Get a random color
                Color tint = colorChoice[tints[i]];
                // Spawn the roses
                Game1.getLocationFromName("Town").dropObject(new StardewValley.Objects.ColoredObject(593, 1, tint), rosesPositions[i] * 64f, Game1.viewport, true, (Farmer)null);
            }
            this.Monitor.Log("Spawned items!", LogLevel.Info);
        }

        private void OnPlayerWarped(object sender, WarpedEventArgs e)
        {
            if (e.NewLocation.Name.Equals("Town"))
            {
                Game1.changeMusicTrack("WizardSong");
                
                if (Game1.player.IsMainPlayer)
                {
                    // Unhook itself
                    helper.Events.Player.Warped -= OnPlayerWarped;
                }
            }
        }
    }
}

// Event: https://stardewvalleywiki.com/Modding:Event_data
// Hookable events: https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Events#Events


//Maps: https://stardewvalleywiki.com/Modding:Maps

//Multiplayer something: https://github.com/Pathoschild/smapi-mod-dump/blob/master/source/%7Ejanavarro95/GeneralMods/HappyBirthday/Framework/MultiplayerSupport.cs
//Game methods: https://github.com/Pathoschild/SMAPI/blob/develop/src/SMAPI/Framework/SGame.cs
//Hats: https://github.com/MouseyPounds/stardew-mods/blob/master/Festival%20of%20the%20Mundane/Source/ShadowFestival/ShadowFestival/ModEntry.cs
//NPC: https://community.playstarbound.com/threads/smapi-stardew-modding-api.108375/page-23
//Custom NPC: https://github.com/janavarro95/Stardew_Valley_Mods/blob/master/GeneralMods/CustomNPCFramework/Class1.cs

//Freeze time: https://github.com/janavarro95/Stardew_Valley_Mods/blob/master/GeneralMods/TimeFreeze/TimeFreeze.cs