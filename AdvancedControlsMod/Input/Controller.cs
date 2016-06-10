﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedControls.Input
{
    public class Controller : IDisposable, IEquatable<Controller>
    {
        private IntPtr device_pointer;
        private IntPtr game_controller;

        private bool is_game_controller;

        private float[] axis_values_raw;
        private float[] axis_values_smooth;
        private float[,] ball_values_raw;
        private float[,] ball_values_smooth;

        public readonly List<string> AxisNames;
        public readonly List<string> BallNames;
        public readonly List<string> HatNames;
        public readonly List<string> ButtonNames;

        public readonly List<Button> Buttons;

        public int Index { get { return SDL.SDL_JoystickInstanceID(device_pointer); } }
        public string Name { get { return SDL.SDL_JoystickName(device_pointer); } }
        public Guid GUID { get { return SDL.SDL_JoystickGetGUID(device_pointer); } }
        public bool Connected { get { return SDL.SDL_JoystickGetAttached(device_pointer) == SDL.SDL_bool.SDL_TRUE; } }
        public bool IsGameController { get { return is_game_controller; } }

        public int NumAxes { get { return SDL.SDL_JoystickNumAxes(device_pointer); } }
        public int NumBalls { get { return SDL.SDL_JoystickNumBalls(device_pointer); } }
        public int NumHats { get { return SDL.SDL_JoystickNumHats(device_pointer); } }
        public int NumButtons { get { return SDL.SDL_JoystickNumButtons(device_pointer); } }

        public static List<Controller> Devices = new List<Controller>();

        public static int NumDevices { get { return Devices.Count;}}

        public static void AddJoystick(int index)
        {
            Devices.Insert(index, new Controller(index, false));
        }

        public static void AddController(int index)
        {
            Devices.Insert(index, new Controller(index, true));
        }

        public static void RemoveDisconnected()
        {
            var remove = new List<Controller>();

            foreach (Controller c in Devices)
                if (!c.Connected) remove.Add(c);

            foreach (Controller c in remove)
            {
                c.Dispose();
                Devices.Remove(c);
            }
        }

        public static Controller Get(int index)
        {
            return Devices[index];
        }

        private Controller(int index, bool is_game_controller)
        {
            if (index > SDL.SDL_NumJoysticks())
                throw new InvalidOperationException("Cannot open controller " + index + " when only " + SDL.SDL_NumJoysticks()+" are connected.");

            this.is_game_controller = is_game_controller;

            if (is_game_controller)
            {
                game_controller = SDL.SDL_GameControllerOpen(index);
                device_pointer = SDL.SDL_GameControllerGetJoystick(game_controller);
            }
            else
            {
                device_pointer = SDL.SDL_JoystickOpen(index);
            }

            Buttons = new List<Button>();

            AxisNames = new List<string>();
            axis_values_raw = new float[SDL.SDL_JoystickNumAxes(device_pointer)];
            axis_values_smooth = new float[SDL.SDL_JoystickNumAxes(device_pointer)];
            for (int i = 0; i < SDL.SDL_JoystickNumAxes(device_pointer); i++)
            {
                string name = null;
                if (is_game_controller)
                    name = SDL.SDL_GameControllerGetStringForAxis((SDL.SDL_GameControllerAxis)i);
                if (!is_game_controller || name == null)
                {
                    if (i == 0) name = "X axis";
                    else if (i == 1) name = "Y axis";
                    else name = "Axis " + (i+1);
                }  
                AxisNames.Add(name);
            }

            BallNames = new List<string>();
            ball_values_raw = new float[SDL.SDL_JoystickNumBalls(device_pointer), 2];
            ball_values_smooth = new float[SDL.SDL_JoystickNumBalls(device_pointer), 2];
            for (int i = 0; i < SDL.SDL_JoystickNumBalls(device_pointer); i++)
                BallNames.Add("Ball " + (i + 1));

            HatNames = new List<string>();
            for (int i = 0; i < SDL.SDL_JoystickNumHats(device_pointer); i++)
            {
                HatNames.Add("Hat " + (i + 1));
                Buttons.Add(new HatButton(this, i, SDL.SDL_HAT_UP));
                Buttons.Add(new HatButton(this, i, SDL.SDL_HAT_DOWN));
                Buttons.Add(new HatButton(this, i, SDL.SDL_HAT_LEFT));
                Buttons.Add(new HatButton(this, i, SDL.SDL_HAT_RIGHT));
            }

            ButtonNames = new List<string>();
            for (int i = 0; i < SDL.SDL_JoystickNumButtons(device_pointer); i++)
            {
                string name = null;
                if (is_game_controller)
                    name = SDL.SDL_GameControllerGetStringForButton((SDL.SDL_GameControllerButton)i);
                if (!is_game_controller || name == null)
                    name = "Button " + (i + 1);
                ButtonNames.Add(name);
                Buttons.Add(new JoystickButton(this, i));
            }

            AdvancedControlsMod.EventManager.OnAxisMotion += UpdateAxis;
            AdvancedControlsMod.EventManager.OnBallMotion += UpdateBall;
            ADVControls.Instance.OnUpdate += Update;

            // Debug
            if (is_game_controller)
                Debug.Log("Game controller connected: " + Name);
            else
                Debug.Log("Joystick connected: " + Name);
            Debug.Log("\tGuid: " + GUID);
            Debug.Log("\tNumber of axes: " + NumAxes);
            Debug.Log("\tNumber of balls: " + NumBalls);
            Debug.Log("\tNumber of hats: " + NumHats);
            Debug.Log("\tNumber of buttons: " + NumButtons);
        }

        private void Update()
        {
            var d = Mathf.Clamp(Time.deltaTime * 12, 0, 1);
            for (int i = 0; i < NumAxes; i++)
            {
                axis_values_smooth[i] = axis_values_smooth[i] * (1 - d) + axis_values_raw[i] * d;
            }
            for (int i = 0; i < NumBalls; i++)
            {
                ball_values_smooth[i, 0] = ball_values_smooth[i, 0] * (1 - d) + ball_values_raw[i, 0] * d;
                ball_values_smooth[i, 1] = ball_values_smooth[i, 1] * (1 - d) + ball_values_raw[i, 1] * d;
            }
        }

        private void UpdateAxis(SDL.SDL_Event e)
        {
            if (e.jdevice.which == Index)
            {
                axis_values_raw[e.jaxis.axis] = e.jaxis.axisValue / 32767.0f;
            }
        }

        private void UpdateBall(SDL.SDL_Event e)
        {
            if (e.jdevice.which == Index)
            {
               ball_values_raw[e.jball.ball, 0] = e.jball.xrel / 32767.0f;
            }
        }

        public float GetAxisSmooth(int index)
        {
            if (index > NumAxes)
                throw new InvalidOperationException("Controller " + Name + " only has " + NumAxes + " axes.");
            return axis_values_smooth[index];
        }

        public float GetAxis(int index)
        {
            if (index > NumAxes)
                throw new InvalidOperationException("Controller " + Name + " only has " + NumAxes + " axes.");
            return axis_values_raw[index];
        }

        public static void AssignMappings()
        {
            try
            {
                var text = System.IO.File.ReadAllText(Application.dataPath + @"\Mods\Resources\AdvancedControls\GameControllerMappings.txt");
                SDL.SDL_GameControllerAddMapping(text);
            }
            catch
            {
                // pass
            }
        }

        public void Dispose()
        {
            if (is_game_controller)
                SDL.SDL_GameControllerClose(game_controller);
            SDL.SDL_JoystickClose(device_pointer);

            AdvancedControlsMod.EventManager.OnAxisMotion -= UpdateAxis;
            ADVControls.Instance.OnUpdate -= Update;
        }

        public bool Equals(Controller other)
        {
            return device_pointer == other.device_pointer;
        }
    }
}
