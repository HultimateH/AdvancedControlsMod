﻿using System;

namespace Lench.AdvancedControls.Input
{
    /// <summary>
    /// Translates a joystick hat position into a button.
    /// </summary>
    public class HatButton : Button
    {
        private Controller _controller;
        private Guid _guid;
        private readonly byte _downState;
        private readonly string _direction;

        private bool _down;
        private bool _pressed;
        private bool _released;

        /// <summary>
        /// Hat button identifying string of the following format:
        /// hat:[index]:[down_state_byte]:[device_guid]
        /// </summary>
        public override string ID => "hat:" + Index + ":" + _downState + ":" + _guid;

        /// <summary>
        /// Guid of the associated controller.
        /// Changing it updates the controller.
        /// </summary>
        public Guid GUID
        {
            get { return _guid; }
            set
            {
                _guid = value;
                _controller = Controller.Get(_guid);
            }
        }

        /// <summary>
        /// Index of the button on a device.
        /// </summary>
        public int Index { get; }

#pragma warning disable CS1591
        public override bool IsDown { get { return _down; } }
        public override bool Pressed { get { return _pressed; } }
        public override bool Released { get { return _released; } }
        public override float Value { get { return _down ? 1 : 0; } }
        public override string Name { get { return _controller != null ? _controller.GetHatName(Index) + " - " + _direction : "Unknown hat" + " - " + _direction; } }
        public override bool Connected { get { return _controller != null && _controller.Connected && Index < _controller.NumHats; } }
#pragma warning restore CS1591

        /// <summary>
        /// Creates a hat button for given controller.
        /// </summary>
        /// <param name="controller">Controller class.</param>
        /// <param name="index">Index of the hat button.</param>
        /// <param name="down_state">Down state byte. For example SDL.SDL_HAT_UP</param>
        public HatButton(Controller controller, int index, byte down_state)
        {
            this._controller = controller;
            this.Index = index;
            this._guid = controller.GUID;
            this._downState = down_state;
            if ((down_state & SDL.SDL_HAT_UP) > 0)
                _direction = "UP";
            else if ((down_state & SDL.SDL_HAT_DOWN) > 0)
                _direction = "DOWN";
            else if ((down_state & SDL.SDL_HAT_LEFT) > 0)
                _direction = "LEFT";
            else if ((down_state & SDL.SDL_HAT_RIGHT) > 0)
                _direction = "RIGHT";
            DeviceManager.OnHatMotion += HandleEvent;
            DeviceManager.OnDeviceAdded += UpdateDevice;
        }

        /// <summary>
        /// Creates a hat button from an identifier string.
        /// Intended for loading buttons from xml.
        /// Throws FormatException.
        /// </summary>
        /// <param name="id">Hat button identifier string.</param>
        public HatButton(string id)
        {
            var args = id.Split(':');
            if (args[0].Equals("hat"))
            {
                Index = int.Parse(args[1]);
                _downState = byte.Parse(args[2]);
                _guid = new Guid(args[3]);
                _controller = Controller.Get(_guid);
            }
            else
                throw new FormatException("Specified ID does not represent a hat button.");

            if ((_downState & SDL.SDL_HAT_UP) > 0)
                _direction = "UP";
            else if ((_downState & SDL.SDL_HAT_DOWN) > 0)
                _direction = "DOWN";
            else if ((_downState & SDL.SDL_HAT_LEFT) > 0)
                _direction = "LEFT";
            else if ((_downState & SDL.SDL_HAT_RIGHT) > 0)
                _direction = "RIGHT";

            DeviceManager.OnHatMotion += HandleEvent;
            DeviceManager.OnDeviceAdded += UpdateDevice;
        }

        private void HandleEvent(SDL.SDL_Event e)
        {
            if (_controller == null) return;
            if (e.jhat.which != _controller.Index &&
                e.jhat.which != _controller.Index)
                return;
            if (e.jhat.hat == Index)
            {
                bool down = (e.jhat.hatValue & _downState) > 0;
                _pressed = this._down != down && down;
                _released = this._down != down && !down;
                this._down = down;
            }
        }

        private void UpdateDevice(SDL.SDL_Event e)
        {
            _controller = Controller.Get(_guid);
        }
    }
}
