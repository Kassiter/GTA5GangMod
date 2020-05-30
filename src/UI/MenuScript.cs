﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA.Native;
using NativeUI;
using System.Windows.Forms;


namespace GTA.GangAndTurfMod {
	/// <summary>
	/// the nativeUI-implementing class
	/// -------------thanks to the NativeUI developers!-------------
	/// </summary>
	public class MenuScript {
        private readonly MenuPool menuPool;

		private readonly UIMenu gangMenu, memberMenu, carMenu;
		
		private UIMenu gangOptionsSubMenu,
			modSettingsSubMenu, warOptionsSubMenu, weaponsMenu, specificGangMemberRegSubMenu, specificCarRegSubMenu;

		private readonly ZonesMenu zonesMenu;
		

		private Ped closestPed;

		private int memberStyle = 0, memberColor = 0;

		private int healthUpgradeCost, armorUpgradeCost, accuracyUpgradeCost, gangValueUpgradeCost;

        private bool savePotentialMembersAsExtended = false;

        private readonly Dictionary<ModOptions.BuyableWeapon, UIMenuCheckboxItem> buyableWeaponCheckboxesDict =
			new Dictionary<ModOptions.BuyableWeapon, UIMenuCheckboxItem>();

        private readonly Dictionary<VehicleColor, UIMenuItem> carColorEntries =
			new Dictionary<VehicleColor, UIMenuItem>();

		private int playerGangOriginalBlipColor = 0;

        private readonly Dictionary<string, int> blipColorEntries = new Dictionary<string, int>
		{
			{"white", 0 },
			{"white-2", 4 },
			{"white snowy", 13 },
			{"red", 1 },
			{"red-2", 6 },
			{"dark red", 76 },
			{"green", 2 },
			{"green-2", 11 },
			{"dark green", 25 },
			{"darker green", 52 },
			{"turquoise", 15 },
			{"blue", 3 },
			{"light blue", 18 },
			{"dark blue", 38 },
			{"darker blue", 54 },
			{"purple", 7 },
			{"purple-2", 19 },
			{"dark purple", 27 },
			{"dark purple-2", 83 },
			{"very dark purple", 58 },
			{"orange", 17 },
			{"orange-2", 51 },
			{"orange-3", 44 },
			{"gray", 20 },
			{"light gray", 39 },
			{"brown", 21 },
			{"beige", 56 },
			{"pink", 23 },
			{"pink-2", 8 },
			{"smooth pink", 41 },
			{"strong pink", 48 },
			{"black", 40 }, //as close as it gets
            {"yellow", 66 },
			{"gold-ish", 28 },
			{"yellow-2", 46 },
			{"light yellow", 33 },
		};

		private UIMenuItem healthButton, armorButton, accuracyButton, upgradeGangValueBtn,
			openGangMenuBtn, openZoneMenuBtn, mindControlBtn, addToGroupBtn, carBackupBtn, paraBackupBtn;

		private int ticksSinceLastCarBkp = 5000, ticksSinceLastParaBkp = 5000;

		public UIMenuListItem aggOption;

		/// <summary>
		/// action invoked when the input field closes
		/// </summary>
		public Action<DesiredInputType, string> OnInputFieldDone;

		public enum DesiredInputType {
			none,
			enterGangName,
			changeKeyBinding,
			enterCustomZoneName,
		}

		public enum ChangeableKeyBinding {
			GangMenuBtn,
			ZoneMenuBtn,
			MindControlBtn,
			AddGroupBtn,
		}

		public DesiredInputType curInputType = DesiredInputType.none;
		public ChangeableKeyBinding targetKeyBindToChange = ChangeableKeyBinding.AddGroupBtn;

		public static MenuScript instance;

		public MenuScript() {
			instance = this;
			
			menuPool = new MenuPool();

			zonesMenu = new ZonesMenu("Gang Mod", "Zone Controls", menuPool);
			memberMenu = new UIMenu("Gang Mod", "Gang Member Registration Controls");
			carMenu = new UIMenu("Gang Mod", "Gang Vehicle Registration Controls");
			gangMenu = new UIMenu("Gang Mod", "Gang Controls");

			menuPool.Add(gangMenu);
			menuPool.Add(memberMenu);
			menuPool.Add(carMenu);

			AddMemberStyleChoices();
			AddSaveMemberButton();
			AddNewPlayerGangMemberButton();
			AddNewEnemyMemberSubMenu();
			AddRemoveGangMemberButton();
			AddRemoveFromAllGangsButton();
			AddMakeFriendlyToPlayerGangButton();

			AddCallBackupBtns();

			AddSaveVehicleButton();
			AddRegisterPlayerVehicleButton();
			AddRegisterEnemyVehicleButton();
			AddRemovePlayerVehicleButton();
			AddRemoveVehicleEverywhereButton();

			//UpdateBuyableWeapons();
			AddWarOptionsSubMenu();
			AddGangOptionsSubMenu();
			AddModSettingsSubMenu();

			gangMenu.RefreshIndex();
			memberMenu.RefreshIndex();

			aggOption.Index = (int)ModOptions.instance.gangMemberAggressiveness;

			//add mouse click as another "select" button
			menuPool.SetKey(UIMenu.MenuControls.Select, Control.PhoneSelect);
			InstructionalButton clickButton = new InstructionalButton(Control.PhoneSelect, "Select");
			zonesMenu.AddInstructionalButton(clickButton);
			gangMenu.AddInstructionalButton(clickButton);
			memberMenu.AddInstructionalButton(clickButton);
			zonesMenu.warAttackStrengthMenu.AddInstructionalButton(clickButton);

			ticksSinceLastCarBkp = ModOptions.instance.ticksCooldownBackupCar;
			ticksSinceLastParaBkp = ModOptions.instance.ticksCooldownParachutingMember;
		}

		#region menu opening methods
		public void OpenGangMenu() {
			if (!menuPool.IsAnyMenuOpen() && curInputType == DesiredInputType.none) {
				UpdateUpgradeCosts();
				//UpdateBuyableWeapons();
				gangMenu.Visible = !gangMenu.Visible;
			}
		}

		public void OpenContextualRegistrationMenu() {
			if (!menuPool.IsAnyMenuOpen() && curInputType == DesiredInputType.none) {
				if (MindControl.CurrentPlayerCharacter.CurrentVehicle == null) {
					closestPed = World.GetClosestPed(MindControl.CurrentPlayerCharacter.Position + MindControl.CurrentPlayerCharacter.ForwardVector * 6.0f, 5.5f);
					if (closestPed != null) {
						UI.ShowSubtitle("ped selected!");
						World.AddExplosion(closestPed.Position, ExplosionType.Steam, 1.0f, 0.1f);
					}
					else {
						UI.ShowSubtitle("Couldn't find a ped in front of you! You have selected yourself.");
						closestPed = MindControl.CurrentPlayerCharacter;
						World.AddExplosion(closestPed.Position, ExplosionType.Extinguisher, 1.0f, 0.1f);
					}

					memberMenu.Visible = !memberMenu.Visible;
				}
				else {
					UI.ShowSubtitle("vehicle selected!");
					carMenu.Visible = !carMenu.Visible;
				}
				RefreshNewEnemyMenuContent();
			}
		}

		public void OpenZoneMenu() {
			if (!menuPool.IsAnyMenuOpen() && curInputType == DesiredInputType.none) {
				ZoneManager.instance.OutputCurrentZoneInfo();
				zonesMenu.UpdateZoneUpgradeBtn();
				zonesMenu.Visible = !zonesMenu.Visible;
			}
		}
		#endregion

		/// <summary>
		/// opens the input field using the provided data.
		/// remember to hide any open menus before calling!
		/// </summary>
		/// <param name="inputType"></param>
		/// <param name="menuCode"></param>
		/// <param name="initialText"></param>
		public void OpenInputField(DesiredInputType inputType, string menuCode, string initialText)
		{
			Function.Call(Hash.DISPLAY_ONSCREEN_KEYBOARD, false, menuCode, "", initialText, "", "", "", 30);
			curInputType = inputType;
		}

		public void Tick() {
			menuPool.ProcessMenus();

			if (curInputType != DesiredInputType.changeKeyBinding && curInputType != DesiredInputType.none) {
				int inputFieldSituation = Function.Call<int>(Hash.UPDATE_ONSCREEN_KEYBOARD);
				if (inputFieldSituation == 1) {
					string typedText = Function.Call<string>(Hash.GET_ONSCREEN_KEYBOARD_RESULT);
					OnInputFieldDone?.Invoke(curInputType, typedText);

					curInputType = DesiredInputType.none;
					

				}
				else if (inputFieldSituation == 2 || inputFieldSituation == 3) {
					curInputType = DesiredInputType.none;
				}
			}

			//countdown for next backups
			ticksSinceLastCarBkp++;
			if (ticksSinceLastCarBkp > ModOptions.instance.ticksCooldownBackupCar)
				ticksSinceLastCarBkp = ModOptions.instance.ticksCooldownBackupCar;
			ticksSinceLastParaBkp++;
			if (ticksSinceLastParaBkp > ModOptions.instance.ticksCooldownParachutingMember)
				ticksSinceLastParaBkp = ModOptions.instance.ticksCooldownParachutingMember;
		}

		void UpdateUpgradeCosts() {
			Gang playerGang = GangManager.instance.PlayerGang;
			healthUpgradeCost = GangCalculations.CalculateHealthUpgradeCost(playerGang.memberHealth);
			armorUpgradeCost = GangCalculations.CalculateArmorUpgradeCost(playerGang.memberArmor);
			accuracyUpgradeCost = GangCalculations.CalculateAccuracyUpgradeCost(playerGang.memberAccuracyLevel);
			gangValueUpgradeCost = GangCalculations.CalculateGangValueUpgradeCost(playerGang.baseTurfValue);

			healthButton.Text = "Upgrade Member Health - " + healthUpgradeCost.ToString();
			armorButton.Text = "Upgrade Member Armor - " + armorUpgradeCost.ToString();
			accuracyButton.Text = "Upgrade Member Accuracy - " + accuracyUpgradeCost.ToString();
			upgradeGangValueBtn.Text = "Upgrade Gang Base Strength - " + gangValueUpgradeCost.ToString();
		}

		

		void FillCarColorEntries() {
			foreach (ModOptions.GangColorTranslation colorList in ModOptions.instance.similarColors) {
				for (int i = 0; i < colorList.vehicleColors.Count; i++) {
					carColorEntries.Add(colorList.vehicleColors[i], new UIMenuItem(colorList.vehicleColors[i].ToString(), "Colors can be previewed if you are inside a vehicle. Click or press enter to confirm the gang color change."));
				}

			}

			if (ModOptions.instance.extraPlayerExclusiveColors == null) {
				ModOptions.instance.SetColorTranslationDefaultValues();
			}
			//and the extra colors, only chooseable by the player!
			foreach (VehicleColor extraColor in ModOptions.instance.extraPlayerExclusiveColors) {
				carColorEntries.Add(extraColor, new UIMenuItem(extraColor.ToString(), "Colors can be previewed if you are inside a vehicle. Click or press enter to confirm the gang color change."));
			}
		}

		public void RefreshKeyBindings() {
			openGangMenuBtn.Text = "Gang Control Key - " + ModOptions.instance.openGangMenuKey.ToString();
			openZoneMenuBtn.Text = "Zone Control Key - " + ModOptions.instance.openZoneMenuKey.ToString();
			addToGroupBtn.Text = "Add or Remove Member from Group - " + ModOptions.instance.addToGroupKey.ToString();
			mindControlBtn.Text = "Take Control of Member - " + ModOptions.instance.mindControlKey.ToString();
		}

		#region Zone Menu Stuff

		
        #endregion


        #region register Member/Vehicle Stuff

        void AddMemberStyleChoices() {
			List<dynamic> memberStyles = new List<dynamic>
			{
				"Business",
				"Street",
				"Beach",
				"Special"
			};

			List<dynamic> memberColors = new List<dynamic>
			{
				"White",
				"Black",
				"Red",
				"Green",
				"Blue",
				"Yellow",
				"Gray",
				"Pink",
				"Purple"
			};

			UIMenuListItem styleList = new UIMenuListItem("Member Dressing Style", memberStyles, 0, "The way the selected member is dressed. Used by the AI when picking members (if the AI gang's chosen style is the same as this member's, it may choose this member).");
			UIMenuListItem colorList = new UIMenuListItem("Member Color", memberColors, 0, "The color the member will be assigned to. Used by the AI when picking members (if the AI gang's color is the same as this member's, it may choose this member).");
            UIMenuCheckboxItem extendedModeToggle = new UIMenuCheckboxItem("Extended Save Mode", savePotentialMembersAsExtended, "If enabled, saves all clothing indexes for non-freemode peds. Can help with some addon peds.");

            memberMenu.AddItem(styleList);
			memberMenu.AddItem(colorList);
            memberMenu.AddItem(extendedModeToggle);
            memberMenu.OnListChange += (sender, item, index) => {
				if (item == styleList) {
					memberStyle = item.Index;
				}
				else if (item == colorList) {
					memberColor = item.Index;
				}

			};

            memberMenu.OnCheckboxChange += (sender, item, checked_) => {
                if (item == extendedModeToggle)
                {
                    savePotentialMembersAsExtended = checked_;
                }
            };

        }

        void AddSaveMemberButton() {
			UIMenuItem newButton = new UIMenuItem("Save Potential Member for future AI gangs", "Saves the selected ped as a potential gang member with the specified data. AI gangs will be able to choose him\\her.");
			memberMenu.AddItem(newButton);
			memberMenu.OnItemSelect += (sender, item, index) => {
				if (item == newButton) {
					if (closestPed.Model == PedHash.FreemodeFemale01 || closestPed.Model == PedHash.FreemodeMale01) {
						if (PotentialGangMember.AddMemberAndSavePool(new FreemodePotentialGangMember(closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor))) {
							UI.ShowSubtitle("Potential freemode member added!");
						}
						else {
							UI.ShowSubtitle("A similar potential member already exists.");
						}
					}
					else {
                        bool addAttempt = savePotentialMembersAsExtended ?
                            PotentialGangMember.AddMemberAndSavePool(new ExtendedPotentialGangMember(closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor)) :
                            PotentialGangMember.AddMemberAndSavePool(new PotentialGangMember(closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor));

                        if (addAttempt) {
							UI.ShowSubtitle("Potential member added!");
						}
						else {
							UI.ShowSubtitle("A similar potential member already exists.");
						}
					}

				}
			};
		}

		void AddNewPlayerGangMemberButton() {
			UIMenuItem newButton = new UIMenuItem("Save ped type for your gang", "Saves the selected ped type as a member of your gang, with the specified data. The selected ped himself won't be a member, however.");
			memberMenu.AddItem(newButton);
			memberMenu.OnItemSelect += (sender, item, index) => {
				if (item == newButton) {
					if (closestPed.Model == PedHash.FreemodeFemale01 || closestPed.Model == PedHash.FreemodeMale01) {
						if (GangManager.instance.PlayerGang.AddMemberVariation(new FreemodePotentialGangMember
					   (closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor))) {
							UI.ShowSubtitle("Freemode Member added successfully!");
						}
						else {
							UI.ShowSubtitle("Your gang already has a similar member.");
						}
					}
					else {
                        bool addAttempt = savePotentialMembersAsExtended ?
                            GangManager.instance.PlayerGang.AddMemberVariation(new ExtendedPotentialGangMember
                       (closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor)) :
                            GangManager.instance.PlayerGang.AddMemberVariation(new PotentialGangMember
                       (closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor));


                        if (addAttempt) {
							UI.ShowSubtitle("Member added successfully!");
						}
						else {
							UI.ShowSubtitle("Your gang already has a similar member.");
						}
					}

				}
			};
		}

		void AddNewEnemyMemberSubMenu() {
			specificGangMemberRegSubMenu = menuPool.AddSubMenu(memberMenu, "Save ped type for a specific enemy gang...");

			specificGangMemberRegSubMenu.OnItemSelect += (sender, item, index) => {
				Gang pickedGang = GangManager.instance.GetGangByName(item.Text);
				if (pickedGang != null) {
					if (closestPed.Model == PedHash.FreemodeFemale01 || closestPed.Model == PedHash.FreemodeMale01) {
						if (pickedGang.AddMemberVariation(new FreemodePotentialGangMember
					   (closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor))) {
							UI.ShowSubtitle("Freemode Member added successfully!");
						}
						else {
							UI.ShowSubtitle("That gang already has a similar member.");
						}
					}
					else {
                        bool addAttempt = savePotentialMembersAsExtended ?
                            pickedGang.AddMemberVariation(new ExtendedPotentialGangMember
                       (closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor)) :
                            pickedGang.AddMemberVariation(new PotentialGangMember
                       (closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor));


                        if (addAttempt)
                        {
                            UI.ShowSubtitle("Member added successfully!");
						}
						else {
							UI.ShowSubtitle("That gang already has a similar member.");
						}
					}
				}
			};

		}

		/// <summary>
		/// removes all options and then adds all gangs that are not controlled by the player as chooseable options in the "Save for a specific gang" submenus
		/// </summary>
		public void RefreshNewEnemyMenuContent() {
			specificGangMemberRegSubMenu.Clear();
			specificCarRegSubMenu.Clear();

			List<Gang> gangsList = GangManager.instance.gangData.gangs;

			for (int i = 0; i < gangsList.Count; i++) {
				if (!gangsList[i].isPlayerOwned) {
					specificGangMemberRegSubMenu.AddItem(new UIMenuItem(gangsList[i].name));
					specificCarRegSubMenu.AddItem(new UIMenuItem(gangsList[i].name));
				}
			}

			specificGangMemberRegSubMenu.RefreshIndex();
			specificCarRegSubMenu.RefreshIndex();

		}

		void AddRemoveGangMemberButton() {
			UIMenuItem newButton = new UIMenuItem("Remove ped type from respective gang", "If the selected ped type was a member of a gang, it will no longer be. The selected ped himself will still be a member, however. This works for your own gang and for the enemies.");
			memberMenu.AddItem(newButton);
			memberMenu.OnItemSelect += (sender, item, index) => {
				if (item == newButton) {
					Gang ownerGang = GangManager.instance.GetGangByRelGroup(closestPed.RelationshipGroup);
					if (ownerGang == null) {
						UI.ShowSubtitle("The ped doesn't seem to be in a gang.", 8000);
						return;
					}
					if (closestPed.Model == PedHash.FreemodeFemale01 || closestPed.Model == PedHash.FreemodeMale01) {
						if (ownerGang.RemoveMemberVariation(new FreemodePotentialGangMember
						(closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor))) {
							UI.ShowSubtitle("Member removed successfully!");
						}
						else {
							UI.ShowSubtitle("The ped doesn't seem to be in a gang.", 8000);
						}
					}
					else {
						if (ownerGang.RemoveMemberVariation(new PotentialGangMember
						(closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor)) ||
                        ownerGang.RemoveMemberVariation(new ExtendedPotentialGangMember
                        (closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor))) {
							UI.ShowSubtitle("Member removed successfully!");
						}
						else {
							UI.ShowSubtitle("The ped doesn't seem to be in a gang.", 8000);
						}
					}
				}
			};
		}

		void AddRemoveFromAllGangsButton() {
			UIMenuItem newButton = new UIMenuItem("Remove ped type from all gangs and pool", "Removes the ped type from all gangs and from the member pool, which means future gangs also won't try to use this type. The selected ped himself will still be a gang member, however.");
			memberMenu.AddItem(newButton);
			memberMenu.OnItemSelect += (sender, item, index) => {
				if (item == newButton) {
					if (closestPed.Model == PedHash.FreemodeFemale01 || closestPed.Model == PedHash.FreemodeMale01) {
						FreemodePotentialGangMember memberToRemove = new FreemodePotentialGangMember
				   (closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor);

						if (PotentialGangMember.RemoveMemberAndSavePool(memberToRemove)) {
							UI.ShowSubtitle("Ped type removed from pool! (It might not be the only similar ped in the pool)");
						}
						else {
							UI.ShowSubtitle("Ped type not found in pool.");
						}

						for (int i = 0; i < GangManager.instance.gangData.gangs.Count; i++) {
							GangManager.instance.gangData.gangs[i].RemoveMemberVariation(memberToRemove);
						}
					}
					else {
						PotentialGangMember memberToRemove = new PotentialGangMember
				   (closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor);
                        ExtendedPotentialGangMember memberToRemoveEx = new ExtendedPotentialGangMember
                   (closestPed, (PotentialGangMember.DressStyle)memberStyle, (PotentialGangMember.MemberColor)memberColor);

                        if (PotentialGangMember.RemoveMemberAndSavePool(memberToRemove) ||
                        PotentialGangMember.RemoveMemberAndSavePool(memberToRemoveEx)) {
							UI.ShowSubtitle("Ped type removed from pool! (It might not be the only similar ped in the pool)");
						}
						else {
							UI.ShowSubtitle("Ped type not found in pool.");
						}

						for (int i = 0; i < GangManager.instance.gangData.gangs.Count; i++) {
							GangManager.instance.gangData.gangs[i].RemoveMemberVariation(memberToRemove);
						}
					}
				}
			};
		}

		void AddMakeFriendlyToPlayerGangButton() {
			UIMenuItem newButton = new UIMenuItem("Make Ped friendly to your gang", "Makes the selected ped (and everyone from his group) and your gang become allies. Can't be used with cops or gangs from this mod! NOTE: this only lasts until scripts are loaded again");
			memberMenu.AddItem(newButton);
			memberMenu.OnItemSelect += (sender, item, index) => {
				if (item == newButton) {
					int closestPedRelGroup = closestPed.RelationshipGroup;
					//check if we can really become allies with this guy
					if (closestPedRelGroup != Function.Call<int>(Hash.GET_HASH_KEY, "COP")) {
						//he can still be from one of the gangs! we should check

						if (GangManager.instance.GetGangByRelGroup(closestPedRelGroup) != null) {
							UI.ShowSubtitle("That ped is a gang member! Gang members cannot be marked as allies");
							return;
						}

						//ok, we can be allies
						Gang playerGang = GangManager.instance.PlayerGang;
						World.SetRelationshipBetweenGroups(Relationship.Respect, playerGang.relationGroupIndex, closestPedRelGroup);
						World.SetRelationshipBetweenGroups(Relationship.Respect, closestPedRelGroup, playerGang.relationGroupIndex);
						UI.ShowSubtitle("That ped's group is now an allied group!");
					}
					else {
						UI.ShowSubtitle("That ped is a cop! Cops cannot be marked as allies");
					}
				}
			};
		}


		void AddSaveVehicleButton() {
			UIMenuItem newButton = new UIMenuItem("Register Vehicle as usable by AI Gangs", "Makes the vehicle type you are driving become chooseable as one of the types used by AI gangs.");
			carMenu.AddItem(newButton);
			carMenu.OnItemSelect += (sender, item, index) => {
				if (item == newButton) {
					Vehicle curVehicle = MindControl.CurrentPlayerCharacter.CurrentVehicle;
					if (curVehicle != null) {
						if (PotentialGangVehicle.AddVehicleAndSavePool(new PotentialGangVehicle(curVehicle.Model.Hash))) {
							UI.ShowSubtitle("Vehicle added to pool!");
						}
						else {
							UI.ShowSubtitle("That vehicle has already been added to the pool.");
						}
					}
					else {
						UI.ShowSubtitle("You are not inside a vehicle.");
					}
				}
			};
		}

		void AddRegisterPlayerVehicleButton() {
			UIMenuItem newButton = new UIMenuItem("Register Vehicle for your Gang", "Makes the vehicle type you are driving become one of the default types used by your gang.");
			carMenu.AddItem(newButton);
			carMenu.OnItemSelect += (sender, item, index) => {
				if (item == newButton) {
					Vehicle curVehicle = MindControl.CurrentPlayerCharacter.CurrentVehicle;
					if (curVehicle != null) {
						if (GangManager.instance.PlayerGang.AddGangCar(new PotentialGangVehicle(curVehicle.Model.Hash))) {
							UI.ShowSubtitle("Gang vehicle added!");
						}
						else {
							UI.ShowSubtitle("That vehicle is already registered for your gang.");
						}
					}
					else {
						UI.ShowSubtitle("You are not inside a vehicle.");
					}
				}
			};
		}

		void AddRegisterEnemyVehicleButton() {
			specificCarRegSubMenu = menuPool.AddSubMenu(carMenu, "Register vehicle for a specific enemy gang...");

			specificCarRegSubMenu.OnItemSelect += (sender, item, index) => {
				Gang pickedGang = GangManager.instance.GetGangByName(item.Text);
				if (pickedGang != null) {
					Vehicle curVehicle = MindControl.CurrentPlayerCharacter.CurrentVehicle;
					if (curVehicle != null) {
						if (pickedGang.AddGangCar(new PotentialGangVehicle(curVehicle.Model.Hash))) {
							UI.ShowSubtitle("Gang vehicle added!");
						}
						else {
							UI.ShowSubtitle("That vehicle is already registered for that gang.");
						}
					}
					else {
						UI.ShowSubtitle("You are not inside a vehicle.");
					}
				}
			};
		}


		void AddRemovePlayerVehicleButton() {
			UIMenuItem newButton = new UIMenuItem("Remove Vehicle Type from your Gang", "Removes the vehicle type you are driving from the possible vehicle types for your gang.");
			carMenu.AddItem(newButton);
			carMenu.OnItemSelect += (sender, item, index) => {
				if (item == newButton) {
					Vehicle curVehicle = MindControl.CurrentPlayerCharacter.CurrentVehicle;
					if (curVehicle != null) {
						if (GangManager.instance.PlayerGang.RemoveGangCar(new PotentialGangVehicle(curVehicle.Model.Hash))) {
							UI.ShowSubtitle("Gang vehicle removed!");
						}
						else {
							UI.ShowSubtitle("That vehicle is not registered for your gang.");
						}
					}
					else {
						UI.ShowSubtitle("You are not inside a vehicle.");
					}
				}
			};
		}

		void AddRemoveVehicleEverywhereButton() {
			UIMenuItem newButton = new UIMenuItem("Remove Vehicle Type from all gangs and pool", "Removes the vehicle type you are driving from the possible vehicle types for all gangs, including yours. Existing gangs will also stop using that car and get another one if needed.");
			carMenu.AddItem(newButton);
			carMenu.OnItemSelect += (sender, item, index) => {
				if (item == newButton) {
					Vehicle curVehicle = MindControl.CurrentPlayerCharacter.CurrentVehicle;
					if (curVehicle != null) {
						PotentialGangVehicle removedVehicle = new PotentialGangVehicle(curVehicle.Model.Hash);

						if (PotentialGangVehicle.RemoveVehicleAndSavePool(removedVehicle)) {
							UI.ShowSubtitle("Vehicle type removed from pool!");
						}
						else {
							UI.ShowSubtitle("Vehicle type not found in pool.");
						}

						for (int i = 0; i < GangManager.instance.gangData.gangs.Count; i++) {
							GangManager.instance.gangData.gangs[i].RemoveGangCar(removedVehicle);
						}
					}
					else {
						UI.ShowSubtitle("You are not inside a vehicle.");
					}
				}
			};
		}

		#endregion

		#region Gang Control Stuff

		public void CallCarBackup(bool showMenu = true) {
			if (ticksSinceLastCarBkp < ModOptions.instance.ticksCooldownBackupCar) {
				UI.ShowSubtitle("You must wait before calling for car backup again! (This is configurable)");
				return;
			}
			if (MindControl.instance.AddOrSubtractMoneyToProtagonist(-ModOptions.instance.costToCallBackupCar, true)) {
				Gang playergang = GangManager.instance.PlayerGang;
				if (ZoneManager.instance.GetZonesControlledByGang(playergang.name).Count > 0) {
					Math.Vector3 destPos = MindControl.SafePositionNearPlayer;

					Math.Vector3 spawnPos = SpawnManager.instance.FindGoodSpawnPointForCar(destPos);

					SpawnedDrivingGangMember spawnedVehicle = SpawnManager.instance.SpawnGangVehicle(GangManager.instance.PlayerGang,
							spawnPos, destPos, true, true);
					if (spawnedVehicle != null) {
						if (showMenu) {
							gangMenu.Visible = !gangMenu.Visible;
						}
						ticksSinceLastCarBkp = 0;
						MindControl.instance.AddOrSubtractMoneyToProtagonist(-ModOptions.instance.costToCallBackupCar);
						UI.ShowSubtitle("A vehicle is on its way!", 1000);
					}
					else {
						UI.ShowSubtitle("There are too many gang members around or you haven't registered any member or car.");
					}
				}
				else {
					UI.ShowSubtitle("You need to have control of at least one territory in order to call for backup.");
				}
			}
			else {
				UI.ShowSubtitle("You need $" + ModOptions.instance.costToCallBackupCar.ToString() + " to call a vehicle!");
			}
		}

		void AddCallBackupBtns() {
			carBackupBtn = new UIMenuItem("Call Backup Vehicle ($" + ModOptions.instance.costToCallBackupCar.ToString() + ")", "Calls one of your gang's vehicles to your position. All passengers leave the vehicle once it arrives.");
			paraBackupBtn = new UIMenuItem("Call Parachuting Member ($" + ModOptions.instance.costToCallParachutingMember.ToString() + ")", "Calls a gang member who parachutes to your position (member survival not guaranteed!).");

			gangMenu.AddItem(carBackupBtn);
			gangMenu.AddItem(paraBackupBtn);
			gangMenu.OnItemSelect += (sender, item, index) => {
				if (item == carBackupBtn) {
					CallCarBackup();

				}

				if (item == paraBackupBtn) {
					if (ticksSinceLastParaBkp < ModOptions.instance.ticksCooldownParachutingMember) {
						UI.ShowSubtitle("You must wait before calling for parachuting backup again! (This is configurable)");
						return;
					}

					if (MindControl.instance.AddOrSubtractMoneyToProtagonist(-ModOptions.instance.costToCallParachutingMember, true)) {
						Gang playergang = GangManager.instance.PlayerGang;
						//only allow spawning if the player has turf
						if (ZoneManager.instance.GetZonesControlledByGang(playergang.name).Count > 0) {
							Ped spawnedPed = SpawnManager.instance.SpawnParachutingMember(GangManager.instance.PlayerGang,
					   MindControl.CurrentPlayerCharacter.Position + Math.Vector3.WorldUp * 50, MindControl.SafePositionNearPlayer);
							if (spawnedPed != null) {
								ticksSinceLastParaBkp = 0;
								MindControl.instance.AddOrSubtractMoneyToProtagonist(-ModOptions.instance.costToCallParachutingMember);
								gangMenu.Visible = !gangMenu.Visible;
							}
							else {
								UI.ShowSubtitle("There are too many gang members around or you haven't registered any member.");
							}
						}
						else {
							UI.ShowSubtitle("You need to have control of at least one territory in order to call for backup.");
						}
					}
					else {
						UI.ShowSubtitle("You need $" + ModOptions.instance.costToCallParachutingMember.ToString() + " to call a parachuting member!");
					}

				}
			};


		}

		void AddWarOptionsSubMenu() {
			warOptionsSubMenu = menuPool.AddSubMenu(gangMenu, "War Options Menu");

			UIMenuItem skipWarBtn = new UIMenuItem("Skip current War",
			   "If a war is currently occurring, it will instantly end, and its outcome will be defined by the strength and reinforcements of the involved gangs and a touch of randomness.");
			UIMenuItem resetAlliedSpawnBtn = new UIMenuItem("Set allied spawn points to your region",
				"If a war is currently occurring, your gang members will keep spawning at the 3 allied spawn points for as long as you've got reinforcements. This option sets all 3 spawn points to your location: one exactly where you are and 2 nearby.");
			UIMenuItem resetEnemySpawnBtn = new UIMenuItem("Force reset enemy spawn points",
				"If a war is currently occurring, the enemy spawn points will be randomly set to a nearby location. Use this if they end up spawning somewhere unreachable.");

			warOptionsSubMenu.AddItem(skipWarBtn);
			warOptionsSubMenu.AddItem(resetAlliedSpawnBtn);

			UIMenuItem[] setSpecificSpawnBtns = new UIMenuItem[3];
			for (int i = 0; i < setSpecificSpawnBtns.Length; i++) {
				setSpecificSpawnBtns[i] = new UIMenuItem(string.Concat("Set allied spawn point ", (i + 1).ToString(), " to your position"),
					string.Concat("If a war is currently occurring, your gang members will keep spawning at the 3 allied spawn points for as long as you've got reinforcements. This option sets spawn point number ",
						(i + 1).ToString(), " to your exact location."));
				warOptionsSubMenu.AddItem(setSpecificSpawnBtns[i]);
			}

			warOptionsSubMenu.AddItem(resetEnemySpawnBtn);

			warOptionsSubMenu.OnItemSelect += (sender, item, index) => {
				if (GangWarManager.instance.isOccurring) {
					if (item == skipWarBtn) {

						GangWarManager.instance.EndWar(GangWarManager.instance.SkipWar(0.9f));
					}
					else

					if (item == resetAlliedSpawnBtn) {
						if (GangWarManager.instance.playerNearWarzone) {
							GangWarManager.instance.ForceSetAlliedSpawnPoints(MindControl.SafePositionNearPlayer);
						}
						else {
							UI.ShowSubtitle("You must be in the contested zone or close to the war blip before setting the spawn point!");
						}
					}
					else

					if (item == resetEnemySpawnBtn) {
						if (GangWarManager.instance.playerNearWarzone) {
							if (GangWarManager.instance.ReplaceEnemySpawnPoint()) {
								UI.ShowSubtitle("Enemy spawn point reset succeeded!");
							}
							else {
								UI.ShowSubtitle("Enemy spawn point reset failed (try again)!");
							}
						}
						else {
							UI.ShowSubtitle("You must be in the contested zone or close to the war blip before resetting spawn points!");
						}
					}
					else {
						for (int i = 0; i < setSpecificSpawnBtns.Length; i++) {
							if (item == setSpecificSpawnBtns[i]) {
								if (GangWarManager.instance.playerNearWarzone) {
									GangWarManager.instance.SetSpecificAlliedSpawnPoint(i, MindControl.SafePositionNearPlayer);
								}
								else {
									UI.ShowSubtitle("You must be in the contested zone or close to the war blip before setting the spawn point!");
								}

								break;
							}

						}
					}


				}
				else {
					UI.ShowSubtitle("There is no war in progress.");
				}

			};

			warOptionsSubMenu.RefreshIndex();
		}

		void AddGangOptionsSubMenu() {
			gangOptionsSubMenu = menuPool.AddSubMenu(gangMenu, "Gang Customization/Upgrades Menu");

			AddGangUpgradesMenu();
			AddGangWeaponsMenu();
			AddSetCarColorMenu();
			AddSetBlipColorMenu();
			AddRenameGangButton();

			gangOptionsSubMenu.RefreshIndex();
		}

		void AddGangUpgradesMenu() {
			UIMenu upgradesMenu = menuPool.AddSubMenu(gangOptionsSubMenu, "Gang Upgrades...");

			//upgrade buttons
			healthButton = new UIMenuItem("Upgrade Member Health - " + healthUpgradeCost.ToString(), "Increases gang member starting and maximum health. The cost increases with the amount of upgrades made. The limit is configurable via the ModOptions file.");
			armorButton = new UIMenuItem("Upgrade Member Armor - " + armorUpgradeCost.ToString(), "Increases gang member starting body armor. The cost increases with the amount of upgrades made. The limit is configurable via the ModOptions file.");
			accuracyButton = new UIMenuItem("Upgrade Member Accuracy - " + accuracyUpgradeCost.ToString(), "Increases gang member firing accuracy. The cost increases with the amount of upgrades made. The limit is configurable via the ModOptions file.");
			upgradeGangValueBtn = new UIMenuItem("Upgrade Gang Base Strength - " + gangValueUpgradeCost.ToString(), "Increases the level territories have after you take them. This level affects the income provided, the reinforcements available in a war and reduces general police presence. The limit is configurable via the ModOptions file.");
			upgradesMenu.AddItem(healthButton);
			upgradesMenu.AddItem(armorButton);
			upgradesMenu.AddItem(accuracyButton);
			upgradesMenu.AddItem(upgradeGangValueBtn);
			upgradesMenu.RefreshIndex();

			upgradesMenu.OnItemSelect += (sender, item, index) => {
				Gang playerGang = GangManager.instance.PlayerGang;

				if (item == healthButton) {
					if (MindControl.instance.AddOrSubtractMoneyToProtagonist(-healthUpgradeCost, true)) {
						if (playerGang.memberHealth < ModOptions.instance.maxGangMemberHealth) {
							playerGang.memberHealth += ModOptions.instance.GetHealthUpgradeIncrement();
							if (playerGang.memberHealth > ModOptions.instance.maxGangMemberHealth) {
								playerGang.memberHealth = ModOptions.instance.maxGangMemberHealth;
							}
							MindControl.instance.AddOrSubtractMoneyToProtagonist(-healthUpgradeCost);
							GangManager.instance.SaveGangData();
							UI.ShowSubtitle("Member health upgraded!");
						}
						else {
							UI.ShowSubtitle("Your members' health is at its maximum limit (it can be configured in the ModOptions file)");
						}
					}
					else {
						UI.ShowSubtitle("You don't have enough money to buy that upgrade.");
					}
				}

				if (item == armorButton) {
					if (MindControl.instance.AddOrSubtractMoneyToProtagonist(-armorUpgradeCost, true)) {
						if (playerGang.memberArmor < ModOptions.instance.maxGangMemberArmor) {
							playerGang.memberArmor += ModOptions.instance.GetArmorUpgradeIncrement();
							if (playerGang.memberArmor > ModOptions.instance.maxGangMemberArmor) {
								playerGang.memberArmor = ModOptions.instance.maxGangMemberArmor;
							}
							MindControl.instance.AddOrSubtractMoneyToProtagonist(-armorUpgradeCost);
							GangManager.instance.SaveGangData();
							UI.ShowSubtitle("Member armor upgraded!");
						}
						else {
							UI.ShowSubtitle("Your members' armor is at its maximum limit (it can be configured in the ModOptions file)");
						}
					}
					else {
						UI.ShowSubtitle("You don't have enough money to buy that upgrade.");
					}
				}

				if (item == accuracyButton) {
					if (MindControl.instance.AddOrSubtractMoneyToProtagonist(-accuracyUpgradeCost, true)) {
						if (playerGang.memberAccuracyLevel < ModOptions.instance.maxGangMemberAccuracy) {
							playerGang.memberAccuracyLevel += ModOptions.instance.GetAccuracyUpgradeIncrement();
							if (playerGang.memberAccuracyLevel > ModOptions.instance.maxGangMemberAccuracy) {
								playerGang.memberAccuracyLevel = ModOptions.instance.maxGangMemberAccuracy;
							}
							MindControl.instance.AddOrSubtractMoneyToProtagonist(-accuracyUpgradeCost);
							GangManager.instance.SaveGangData();
							UI.ShowSubtitle("Member accuracy upgraded!");
						}
						else {
							UI.ShowSubtitle("Your members' accuracy is at its maximum limit (it can be configured in the ModOptions file)");
						}
					}
					else {
						UI.ShowSubtitle("You don't have enough money to buy that upgrade.");
					}
				}

				if (item == upgradeGangValueBtn) {
					if (MindControl.instance.AddOrSubtractMoneyToProtagonist(-gangValueUpgradeCost, true)) {
						if (playerGang.baseTurfValue < ModOptions.instance.maxTurfValue) {
							playerGang.baseTurfValue++;
							if (playerGang.baseTurfValue > ModOptions.instance.maxTurfValue) {
								playerGang.baseTurfValue = ModOptions.instance.maxTurfValue;
							}
							MindControl.instance.AddOrSubtractMoneyToProtagonist(-gangValueUpgradeCost);
							GangManager.instance.SaveGangData();
							UI.ShowSubtitle("Gang Base Strength upgraded!");
						}
						else {
							UI.ShowSubtitle("Your Gang Base Strength is at its maximum limit (it can be configured in the ModOptions file)");
						}
					}
					else {
						UI.ShowSubtitle("You don't have enough money to buy that upgrade.");
					}
				}

				UpdateUpgradeCosts();

			};

		}

		void AddGangWeaponsMenu() {
			weaponsMenu = menuPool.AddSubMenu(gangOptionsSubMenu, "Gang Weapons Menu");

			Gang playerGang = GangManager.instance.PlayerGang;

			gangOptionsSubMenu.OnMenuChange += (oldMenu, newMenu, forward) => {
				if (newMenu == weaponsMenu) {
					RefreshBuyableWeaponsMenuContent();
				}
			};

			weaponsMenu.OnCheckboxChange += (sender, item, checked_) => {
				foreach (KeyValuePair<ModOptions.BuyableWeapon, UIMenuCheckboxItem> kvp in buyableWeaponCheckboxesDict) {
					if (kvp.Value == item) {
						if (playerGang.gangWeaponHashes.Contains(kvp.Key.wepHash)) {
							playerGang.gangWeaponHashes.Remove(kvp.Key.wepHash);
							MindControl.instance.AddOrSubtractMoneyToProtagonist(kvp.Key.price);
							GangManager.instance.SaveGangData();
							UI.ShowSubtitle("Weapon Removed!");
							item.Checked = false;
						}
						else {
							if (MindControl.instance.AddOrSubtractMoneyToProtagonist(-kvp.Key.price)) {
								playerGang.gangWeaponHashes.Add(kvp.Key.wepHash);
								GangManager.instance.SaveGangData();
								UI.ShowSubtitle("Weapon Bought!");
								item.Checked = true;
							}
							else {
								UI.ShowSubtitle("You don't have enough money to buy that weapon for your gang.");
								item.Checked = false;
							}
						}

						break;
					}
				}

			};
		}

		/// <summary>
		/// removes all options and then adds all gangs that are not controlled by the player as chooseable options in the "Save ped type for a specific gang..." submenu
		/// </summary>
		public void RefreshBuyableWeaponsMenuContent() {
			weaponsMenu.Clear();

			buyableWeaponCheckboxesDict.Clear();

			List<ModOptions.BuyableWeapon> weaponsList = ModOptions.instance.buyableWeapons;

			Gang playerGang = GangManager.instance.PlayerGang;

			for (int i = 0; i < weaponsList.Count; i++) {
				UIMenuCheckboxItem weaponCheckBox = new UIMenuCheckboxItem
						(string.Concat(weaponsList[i].wepHash.ToString(), " - ", weaponsList[i].price.ToString()),
						playerGang.gangWeaponHashes.Contains(weaponsList[i].wepHash));
				buyableWeaponCheckboxesDict.Add(weaponsList[i], weaponCheckBox);
				weaponsMenu.AddItem(weaponCheckBox);
			}

			weaponsMenu.RefreshIndex();
		}

		void AddSetCarColorMenu() {
			FillCarColorEntries();

			UIMenu carColorsMenu = menuPool.AddSubMenu(gangOptionsSubMenu, "Gang Vehicle Colors Menu");

			Gang playerGang = GangManager.instance.PlayerGang;

			VehicleColor[] carColorsArray = carColorEntries.Keys.ToArray();
			UIMenuItem[] colorButtonsArray = carColorEntries.Values.ToArray();

			for (int i = 0; i < colorButtonsArray.Length; i++) {
				carColorsMenu.AddItem(colorButtonsArray[i]);
			}

			carColorsMenu.RefreshIndex();

			carColorsMenu.OnIndexChange += (sender, index) => {
				Vehicle playerVehicle = MindControl.CurrentPlayerCharacter.CurrentVehicle;
				if (playerVehicle != null) {
					playerVehicle.PrimaryColor = carColorsArray[index];
				}
			};

			carColorsMenu.OnItemSelect += (sender, item, checked_) => {
				for (int i = 0; i < carColorsArray.Length; i++) {
					if (item == carColorEntries[carColorsArray[i]]) {
						playerGang.vehicleColor = carColorsArray[i];
						GangManager.instance.SaveGangData(false);
						UI.ShowSubtitle("Gang vehicle color changed!");
						break;
					}
				}

			};
		}

		void AddSetBlipColorMenu() {

			UIMenu blipColorsMenu = menuPool.AddSubMenu(gangOptionsSubMenu, "Gang Blip Colors Menu");

			Gang playerGang = GangManager.instance.PlayerGang;

			string[] blipColorNamesArray = blipColorEntries.Keys.ToArray();
			int[] colorCodesArray = blipColorEntries.Values.ToArray();

			for (int i = 0; i < colorCodesArray.Length; i++) {
				blipColorsMenu.AddItem(new UIMenuItem(blipColorNamesArray[i], "The color change can be seen immediately on turf blips. Click or press enter after selecting a color to save the color change."));
			}

			blipColorsMenu.RefreshIndex();

			blipColorsMenu.OnIndexChange += (sender, index) => {
				GangManager.instance.PlayerGang.blipColor = colorCodesArray[index];
				ZoneManager.instance.RefreshZoneBlips();
			};

			gangOptionsSubMenu.OnMenuChange += (oldMenu, newMenu, forward) => {
				if (newMenu == blipColorsMenu) {
					playerGangOriginalBlipColor = GangManager.instance.PlayerGang.blipColor;
				}
			};

			blipColorsMenu.OnMenuClose += (sender) => {
				GangManager.instance.PlayerGang.blipColor = playerGangOriginalBlipColor;
				ZoneManager.instance.RefreshZoneBlips();
			};

			blipColorsMenu.OnItemSelect += (sender, item, checked_) => {
				for (int i = 0; i < blipColorNamesArray.Length; i++) {
					if (item.Text == blipColorNamesArray[i]) {
						GangManager.instance.PlayerGang.blipColor = colorCodesArray[i];
						playerGangOriginalBlipColor = colorCodesArray[i];
						GangManager.instance.SaveGangData(false);
						UI.ShowSubtitle("Gang blip color changed!");
						break;
					}
				}

			};
		}

		void AddRenameGangButton() {
			UIMenuItem newButton = new UIMenuItem("Rename Gang", "Resets your gang's name.");
			gangOptionsSubMenu.AddItem(newButton);
			gangOptionsSubMenu.OnItemSelect += (sender, item, index) => {
				if (item == newButton) {
					gangOptionsSubMenu.Visible = !gangOptionsSubMenu.Visible;
					OpenInputField(DesiredInputType.enterGangName, "FMMC_KEY_TIP12N", GangManager.instance.PlayerGang.name);
				}
			};

			OnInputFieldDone += (inputType, typedText) =>
			{
				if(inputType == DesiredInputType.enterGangName)
				{
					if (typedText != "none" && GangManager.instance.GetGangByName(typedText) == null)
					{
						ZoneManager.instance.GiveGangZonesToAnother(GangManager.instance.PlayerGang.name, typedText);
						GangManager.instance.PlayerGang.name = typedText;
						GangManager.instance.SaveGangData();

						UI.ShowSubtitle("Your gang is now known as the " + typedText);
					}
					else
					{
						UI.ShowSubtitle("That name is not allowed, sorry! (It may be in use already)");
					}
				}
			};
		}

		#endregion

		#region Mod Settings

		void AddModSettingsSubMenu() {
			modSettingsSubMenu = menuPool.AddSubMenu(gangMenu, "Mod Settings Menu");

			AddNotificationsToggle();
			AddMemberAggressivenessControl();
			AddEnableAmbientSpawnToggle();
			AddAiExpansionToggle();
			AddShowMemberBlipsToggle();
			AddMeleeOnlyToggle();
			AddEnableWarVersusPlayerToggle();
			AddEnableCarTeleportToggle();
			AddGangsStartWithPistolToggle();
			AddKeyBindingMenu();
			AddGamepadControlsToggle();
			AddPlayerSpectatorToggle();
			AddForceAIGangsTickButton();
			AddForceAIAttackButton();
			AddReloadOptionsButton();
			AddResetWeaponOptionsButton();
			AddResetOptionsButton();

			modSettingsSubMenu.RefreshIndex();
		}

		void AddNotificationsToggle() {
			UIMenuCheckboxItem notifyToggle = new UIMenuCheckboxItem("Notifications enabled?", ModOptions.instance.notificationsEnabled, "Enables/disables the displaying of messages whenever a gang takes over a zone.");

			modSettingsSubMenu.AddItem(notifyToggle);
			modSettingsSubMenu.OnCheckboxChange += (sender, item, checked_) => {
				if (item == notifyToggle) {
					ModOptions.instance.notificationsEnabled = checked_;
					ModOptions.instance.SaveOptions(false);
				}

			};
		}

		void AddMemberAggressivenessControl() {
			List<dynamic> aggModes = new List<dynamic>
			{
				"V. Aggressive",
				"Aggressive",
				"Defensive"
			};

			aggOption = new UIMenuListItem("Member Aggressiveness", aggModes, (int)ModOptions.instance.gangMemberAggressiveness, "This controls how aggressive members from all gangs will be. Very aggressive members will shoot at cops and other gangs on sight, aggressive members will shoot only at other gangs on sight and defensive members will only shoot when one of them is attacked or aimed at.");
			modSettingsSubMenu.AddItem(aggOption);
			modSettingsSubMenu.OnListChange += (sender, item, index) => {
				if (item == aggOption) {
					ModOptions.instance.SetMemberAggressiveness((ModOptions.GangMemberAggressivenessMode)index);
				}
			};
		}

		void AddAiExpansionToggle() {
			UIMenuCheckboxItem aiToggle = new UIMenuCheckboxItem("Prevent AI Gangs' Expansion?", ModOptions.instance.preventAIExpansion, "If checked, AI Gangs won't start wars or take neutral zones.");

			modSettingsSubMenu.AddItem(aiToggle);
			modSettingsSubMenu.OnCheckboxChange += (sender, item, checked_) => {
				if (item == aiToggle) {
					ModOptions.instance.preventAIExpansion = checked_;
					ModOptions.instance.SaveOptions(false);
				}

			};
		}

		void AddMeleeOnlyToggle() {
			UIMenuCheckboxItem meleeToggle = new UIMenuCheckboxItem("Gang members use melee weapons only?", ModOptions.instance.membersSpawnWithMeleeOnly, "If checked, all gang members will spawn with melee weapons only, even if they purchase firearms or are set to start with pistols.");

			modSettingsSubMenu.AddItem(meleeToggle);
			modSettingsSubMenu.OnCheckboxChange += (sender, item, checked_) => {
				if (item == meleeToggle) {
					ModOptions.instance.membersSpawnWithMeleeOnly = checked_;
					ModOptions.instance.SaveOptions(false);
				}

			};
		}

		void AddEnableWarVersusPlayerToggle() {
			UIMenuCheckboxItem warToggle = new UIMenuCheckboxItem("Enemy gangs can attack your turf?", ModOptions.instance.warAgainstPlayerEnabled, "If unchecked, enemy gangs won't start a war against you, but you will still be able to start a war against them.");

			modSettingsSubMenu.AddItem(warToggle);
			modSettingsSubMenu.OnCheckboxChange += (sender, item, checked_) => {
				if (item == warToggle) {
					ModOptions.instance.warAgainstPlayerEnabled = checked_;
					ModOptions.instance.SaveOptions(false);
				}

			};
		}

		void AddEnableAmbientSpawnToggle() {
			UIMenuCheckboxItem spawnToggle = new UIMenuCheckboxItem("Ambient member spawning?", ModOptions.instance.ambientSpawningEnabled, "If enabled, members from the gang which owns the zone you are in will spawn once in a while. This option does not affect member spawning via backup calls or gang wars.");

			modSettingsSubMenu.AddItem(spawnToggle);
			modSettingsSubMenu.OnCheckboxChange += (sender, item, checked_) => {
				if (item == spawnToggle) {
					ModOptions.instance.ambientSpawningEnabled = checked_;
					ModOptions.instance.SaveOptions(false);
				}

			};
		}

		void AddShowMemberBlipsToggle() {
			UIMenuCheckboxItem blipToggle = new UIMenuCheckboxItem("Show Member and Car Blips?", ModOptions.instance.showGangMemberBlips, "If disabled, members and cars won't spawn with blips attached to them. (This option only affects those that spawn after the option is set)");

			modSettingsSubMenu.AddItem(blipToggle);
			modSettingsSubMenu.OnCheckboxChange += (sender, item, checked_) => {
				if (item == blipToggle) {
					ModOptions.instance.showGangMemberBlips = checked_;
					ModOptions.instance.SaveOptions(false);
				}

			};
		}

		void AddEnableCarTeleportToggle() {
			UIMenuCheckboxItem spawnToggle = new UIMenuCheckboxItem("Backup cars can teleport to always arrive?", ModOptions.instance.forceSpawnCars, "If enabled, backup cars, after taking too long to get to the player, will teleport close by. This will only affect friendly vehicles.");

			modSettingsSubMenu.AddItem(spawnToggle);
			modSettingsSubMenu.OnCheckboxChange += (sender, item, checked_) => {
				if (item == spawnToggle) {
					ModOptions.instance.forceSpawnCars = checked_;
					ModOptions.instance.SaveOptions(false);
				}

			};
		}

		void AddGangsStartWithPistolToggle() {
			UIMenuCheckboxItem pistolToggle = new UIMenuCheckboxItem("Gangs start with Pistols?", ModOptions.instance.gangsStartWithPistols, "If checked, all gangs, except the player's, will start with pistols. Pistols will not be given to gangs already in town.");

			modSettingsSubMenu.AddItem(pistolToggle);
			modSettingsSubMenu.OnCheckboxChange += (sender, item, checked_) => {
				if (item == pistolToggle) {
					ModOptions.instance.gangsStartWithPistols = checked_;
					ModOptions.instance.SaveOptions(false);
				}

			};
		}

		void AddPlayerSpectatorToggle()
		{
			UIMenuCheckboxItem spectatorToggle = new UIMenuCheckboxItem("Player Is a Spectator", ModOptions.instance.playerIsASpectator, "If enabled, all gangs should ignore the player, even during wars.");

			modSettingsSubMenu.AddItem(spectatorToggle);
			modSettingsSubMenu.OnCheckboxChange += (sender, item, checked_) => {
				if (item == spectatorToggle)
				{
					ModOptions.instance.playerIsASpectator = checked_;
					ModOptions.instance.SaveOptions(false);
					GangManager.instance.SetGangRelationsAccordingToAggrLevel(ModOptions.instance.gangMemberAggressiveness);
				}

			};
		}

		void AddGamepadControlsToggle() {
			UIMenuCheckboxItem padToggle = new UIMenuCheckboxItem("Use joypad controls?", ModOptions.instance.joypadControls, "Enables/disables the use of joypad commands to recruit members (pad right), call backup (pad left) and output zone info (pad up). Commands are used while aiming. All credit goes to zixum.");

			modSettingsSubMenu.AddItem(padToggle);
			modSettingsSubMenu.OnCheckboxChange += (sender, item, checked_) => {
				if (item == padToggle) {
					ModOptions.instance.joypadControls = checked_;
					if (checked_) {
						UI.ShowSubtitle("Joypad controls activated. Remember to disable them when not using a joypad, as it is possible to use the commands with mouse/keyboard as well");
					}
					ModOptions.instance.SaveOptions(false);
				}

			};
		}

		void AddKeyBindingMenu() {
			UIMenu bindingsMenu = menuPool.AddSubMenu(modSettingsSubMenu, "Key Bindings...");

			//the buttons
			openGangMenuBtn = new UIMenuItem("Gang Control Key - " + ModOptions.instance.openGangMenuKey.ToString(), "The key used to open the Gang/Mod Menu. Used with shift to open the Member Registration Menu. Default is B.");
			openZoneMenuBtn = new UIMenuItem("Zone Control Key - " + ModOptions.instance.openZoneMenuKey.ToString(), "The key used to check the current zone's name and ownership. Used with shift to open the Zone Menu and with control to toggle zone blip display modes. Default is N.");
			addToGroupBtn = new UIMenuItem("Add or Remove Member from Group - " + ModOptions.instance.addToGroupKey.ToString(), "The key used to add/remove the targeted friendly gang member to/from your group. Members of your group will follow you. Default is H.");
			mindControlBtn = new UIMenuItem("Take Control of Member - " + ModOptions.instance.mindControlKey.ToString(), "The key used to take control of the targeted friendly gang member. Pressing this key while already in control of a member will restore protagonist control. Default is J.");
			bindingsMenu.AddItem(openGangMenuBtn);
			bindingsMenu.AddItem(openZoneMenuBtn);
			bindingsMenu.AddItem(addToGroupBtn);
			bindingsMenu.AddItem(mindControlBtn);
			bindingsMenu.RefreshIndex();

			bindingsMenu.OnItemSelect += (sender, item, index) => {
				UI.ShowSubtitle("Press the new key for this command.");
				curInputType = DesiredInputType.changeKeyBinding;

				if (item == openGangMenuBtn) {
					targetKeyBindToChange = ChangeableKeyBinding.GangMenuBtn;
				}
				if (item == openZoneMenuBtn) {
					targetKeyBindToChange = ChangeableKeyBinding.ZoneMenuBtn;
				}
				if (item == addToGroupBtn) {
					targetKeyBindToChange = ChangeableKeyBinding.AddGroupBtn;
				}
				if (item == mindControlBtn) {
					targetKeyBindToChange = ChangeableKeyBinding.MindControlBtn;
				}
			};
		}

		void AddForceAIGangsTickButton() {
			UIMenuItem newButton = new UIMenuItem("Run an Update on all AI Gangs", "Makes all AI Gangs try to upgrade themselves and/or invade other territories immediately. Their normal updates, which happen from time to time (configurable in the ModOptions file), will still happen normally after this.");
			modSettingsSubMenu.AddItem(newButton);
			modSettingsSubMenu.OnItemSelect += (sender, item, index) => {
				if (item == newButton) {
					GangManager.instance.ForceTickAIGangs();
				}
			};
		}

		void AddForceAIAttackButton() {
			UIMenuItem newButton = new UIMenuItem("Force an AI Gang to Attack this zone", "If you control the current zone, makes a random AI Gang attack it, starting a war. The AI gang won't spend money to make this attack.");
			modSettingsSubMenu.AddItem(newButton);
			modSettingsSubMenu.OnItemSelect += (sender, item, index) => {
				if (item == newButton) {
					GangAI enemyAttackerAI = RandoMath.GetRandomElementFromList(GangManager.instance.enemyGangs);
					if (enemyAttackerAI != null) {
						TurfZone curZone = ZoneManager.instance.GetCurrentTurfZone();
						if (curZone != null) {
							if (curZone.ownerGangName == GangManager.instance.PlayerGang.name) {
								if (!GangWarManager.instance.StartWar(enemyAttackerAI.watchedGang, curZone,
									GangWarManager.WarType.defendingFromEnemy, GangWarManager.AttackStrength.medium)) {
									UI.ShowSubtitle("Couldn't start a war. Is a war already in progress?");
								}
							}
							else {
								UI.ShowSubtitle("The zone you are in is not controlled by your gang.");
							}
						}
						else {
							UI.ShowSubtitle("The zone you are in has not been marked as takeable.");
						}
					}
					else {
						UI.ShowSubtitle("There aren't any enemy gangs in San Andreas!");
					}
				}
			};
		}

		void AddReloadOptionsButton() {
			UIMenuItem newButton = new UIMenuItem("Reload Mod Options", "Reload the settings defined by the ModOptions file. Use this if you tweaked the ModOptions file while playing for its new settings to take effect.");
			modSettingsSubMenu.AddItem(newButton);
			modSettingsSubMenu.OnItemSelect += (sender, item, index) => {
				if (item == newButton) {
					ModOptions.instance.LoadOptions();
					GangManager.instance.ResetGangUpdateIntervals();
					GangManager.instance.AdjustGangsToModOptions();
					UpdateUpgradeCosts();
					carBackupBtn.Text = "Call Backup Vehicle ($" + ModOptions.instance.costToCallBackupCar.ToString() + ")";
					this.paraBackupBtn.Text = "Call Parachuting Member ($" + ModOptions.instance.costToCallParachutingMember.ToString() + ")";
					zonesMenu.UpdateTakeOverBtnText();
				}
			};
		}

		void AddResetWeaponOptionsButton() {
			UIMenuItem newButton = new UIMenuItem("Reset Weapon List and Prices to Defaults", "Resets the weapon list in the ModOptions file back to the default values. The new options take effect immediately.");
			modSettingsSubMenu.AddItem(newButton);
			modSettingsSubMenu.OnItemSelect += (sender, item, index) => {
				if (item == newButton) {
					ModOptions.instance.buyableWeapons.Clear();
					ModOptions.instance.SetWeaponListDefaultValues();
					ModOptions.instance.SaveOptions(false);
					UpdateUpgradeCosts();
				}
			};
		}

		void AddResetOptionsButton() {
			UIMenuItem newButton = new UIMenuItem("Reset Mod Options to Defaults", "Resets all the options in the ModOptions file back to the default values (except the possible gang first and last names). The new options take effect immediately.");
			modSettingsSubMenu.AddItem(newButton);
			modSettingsSubMenu.OnItemSelect += (sender, item, index) => {
				if (item == newButton) {
					ModOptions.instance.SetAllValuesToDefault();
					UpdateUpgradeCosts();
					zonesMenu.UpdateTakeOverBtnText();
				}
			};
		}

		#endregion
	}
}
