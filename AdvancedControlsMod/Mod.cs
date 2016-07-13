﻿using System;
using System.Reflection;
using spaar.ModLoader;
using Lench.Scripter;
using Lench.AdvancedControls.UI;
using Lench.AdvancedControls.Input;
using Lench.AdvancedControls.Controls;
using System.Collections.Generic;
using UnityEngine;

namespace Lench.AdvancedControls
{
    public class AdvancedControlsMod : Mod
    {
        public override string Name { get; } = "AdvancedControlsMod";
        public override string DisplayName { get; } = "Advanced Controls Mod";
        public override string Author { get; } = "Lench";
        public override Version Version
        {
            get
            {
                var v =  Assembly.GetExecutingAssembly().GetName().Version;
                return new Version(v.Major, v.Minor, v.Build);
            }
        }
        
        public override string VersionExtra { get; } = "";
        public override string BesiegeVersion { get; } = "v0.3";
        public override bool CanBeUnloaded { get; } = true;
        public override bool Preload { get; } = false;

        public override void OnLoad()
        {
            UnityEngine.Object.DontDestroyOnLoad(ACM.Instance);
            BlockHandlers.OnInitialisation += ACM.Instance.Initialise;
            XmlSaver.OnSave += MachineData.Save;
            XmlLoader.OnLoad += MachineData.Load;
        }

        public override void OnUnload()
        {
            BlockHandlers.OnInitialisation -= ACM.Instance.Initialise;
            XmlSaver.OnSave -= MachineData.Save;
            XmlLoader.OnLoad -= MachineData.Load;
            Configuration.Save();

            UnityEngine.Object.Destroy(ACM.Instance);
        }
    }

    public class ACM : SingleInstance<ACM>
    {
        public override string Name { get { return "Advanced Controls"; } }

        public bool ModEnabled = true;
        public bool DBUpdaterEnabled = false;
        public bool ModUpdaterEnabled = false;

        internal bool LoadedMachine = false;

        internal ControlMapper ControlMapper;
        internal DeviceManager DeviceManager;

        internal delegate void UpdateEventHandler();
        internal event UpdateEventHandler OnUpdate;

        internal delegate void InitialiseEventHandler();
        internal event InitialiseEventHandler OnInitialisation;

        private Guid copy_source;

        private void Start()
        {
            ControlMapper = gameObject.AddComponent<ControlMapper>();
            DeviceManager = gameObject.AddComponent<DeviceManager>();

            if (PythonEnvironment.Loaded)
            {
                PythonEnvironment.AddInitStatement("clr.AddReference(\"AdvancedControlsMod\")");
                PythonEnvironment.AddInitStatement("from AdvancedControls import AdvancedControls");
                PythonEnvironment.AddInitStatement("from AdvancedControls.Axes import AxisType");
                PythonEnvironment.AddInitStatement("from AdvancedControls.Axes.ChainAxis import ChainMethod");
            }

            Configuration.Load();

            if (ModUpdaterEnabled)
                CheckForModUpdate();

            if (DBUpdaterEnabled)
                CheckForDBUpdate();

            Commands.RegisterCommand("controller", ControllerCommand, "Enter 'controller' for all available controller commands.");
            Commands.RegisterCommand("acm", ConfigurationCommand, "Enter 'acm' for all available configuration commands.");
            SettingsMenu.RegisterSettingsButton("ACM", EnableToggle, ModEnabled, 12);
        }

        private void OnDestroy()
        {
            OnUpdate = null;
            OnInitialisation = null;
            Destroy(ControlMapper);
            Destroy(DeviceManager);
            Destroy(GameObject.Find("Advanced Controls").transform.gameObject);
        }

        private void Update()
        {
            if (!ModEnabled) return;

            if (BlockMapper.CurrentInstance != null)
            {
                if (BlockMapper.CurrentInstance.Block != null && BlockMapper.CurrentInstance.Block != ControlMapper.Block)
                    ControlMapper.ShowBlockControls(BlockMapper.CurrentInstance.Block);

                if (BlockMapper.CurrentInstance.Block != null &&
                    UnityEngine.Input.GetKey(KeyCode.LeftControl) ||
                    UnityEngine.Input.GetKey(KeyCode.LeftCommand))
                {
                    if (UnityEngine.Input.GetKey(KeyCode.C))
                        copy_source = BlockMapper.CurrentInstance.Block.Guid;
                    if (copy_source != null && UnityEngine.Input.GetKey(KeyCode.V))
                        ControlManager.CopyBlockControls(copy_source, BlockMapper.CurrentInstance.Block.Guid);
                }
            }
            else
            {
                if (ControlMapper.Visible)
                    ControlMapper.Hide();
            }

            if (LoadedMachine)
            {
                LoadedMachine = false;
                ControlOverview.Open(true);
            }

            OnUpdate?.Invoke();
        }

        internal void Initialise()
        {
            if (!ModEnabled) return;
            OnInitialisation?.Invoke();
        }

        private void EnableToggle(bool active)
        {
            ModEnabled = active;
        }

        private string ControllerCommand(string[] args, IDictionary<string, string> namedArgs)
        {
            if (args.Length > 0)
            {
                switch (args[0].ToLower())
                {
                    case "list":
                        string result = "Controller list:\n";
                        if (Controller.NumDevices > 0)
                            for (int i = 0; i < Controller.NumDevices; i++)
                            {
                                var controller = Controller.Get(i);
                                result += i+": "+controller.Name+" ("+(controller.IsGameController ? "Controller" : "Joystick")+")\n"+ "\tGuid: " + controller.GUID+"\n";
                            }
                        else
                            result = "No devices connected.";
                        return result;
                    case "info":

                    default:
                        return "Invalid command. Enter 'controller' for all available commands.";
                }
            }
            else
            {
                return "Available commands:\n" +
                    "  controller list             \t List all connected devices.\n" +
                    "  controller info [index]     \t Show info of a device at index.\n";
            }
        }

        private string ConfigurationCommand(string[] args, IDictionary<string, string> namedArgs)
        {
            if (args.Length > 0)
            {
                switch (args[0].ToLower())
                {
                    case "modupdate":
                        if (args.Length > 1)
                        {
                            switch (args[1].ToLower())
                            {
                                case "check":
                                    CheckForModUpdate(true);
                                    return "Checking for mod updates ...";
                                case "enable":
                                    ModUpdaterEnabled = true;
                                    return "Mod update checker enabled.";
                                case "disable":
                                    ModUpdaterEnabled = false;
                                    return "Mod update checker disabled.";
                                default:
                                    return "Invalid argument [check/enable/disable]. Enter 'acm' for all available commands.";
                            }
                        }
                        else
                        {
                            return "Missing argument [check/enable/disable]. Enter 'acm' for all available commands.";
                        }
                    case "dbupdate":
                        if (args.Length > 1)
                        {
                            switch (args[1].ToLower())
                            {
                                case "check":
                                    CheckForDBUpdate(true);
                                    return "Checking for controller DB updates ...";
                                case "enable":
                                    DBUpdaterEnabled = true;
                                    return "Controller DB update checker enabled.";
                                case "disable":
                                    DBUpdaterEnabled = false;
                                    return "Controller DB update checker disabled.";
                                default:
                                    return "Invalid argument [check/enable/disable]. Enter 'acm' for all available commands.";
                            }
                        }
                        else
                        {
                            return "Missing argument [check/enable/disable]. Enter 'acm' for all available commands.";
                        }
                    default:
                        return "Invalid command. Enter 'acm' for all available commands.";
                }
            }
            else
            {
                return "Available commands:\n" +
                    "  acm modupdate check  \t Checks for mod update.\n" +
                    "  acm modupdate enable \t Enables update checker.\n" +
                    "  acm modupdate disable\t Disables update checker.\n" +
                    "  acm dbupdate check   \t Checks for controller database update.\n" +
                    "  acm dbupdate enable  \t Enables automatic controller database updates.\n" +
                    "  acm dbupdate disable \t Disables automatic controller database updates.\n";
            }
        }

        private void CheckForModUpdate(bool verbose = false)
        {
            var updater = gameObject.AddComponent<Updater>();
            updater.Check(
                "Advanced Controls Mod",
                "https://api.github.com/repos/lench4991/AdvancedControlsMod/releases",
                Assembly.GetExecutingAssembly().GetName().Version,
                new List<Updater.Link>()
                    {
                            new Updater.Link() { DisplayName = "Spiderling forum page", URL = "http://forum.spiderlinggames.co.uk/index.php?threads/3150/" },
                            new Updater.Link() { DisplayName = "GitHub release page", URL = "https://github.com/lench4991/AdvancedControlsMod/releases/latest" }
                    },
                verbose);
        }

        private void CheckForDBUpdate(bool verbose = false)
        {
            StartCoroutine(DeviceManager.AssignMappings(true, verbose));
        }
    }
}