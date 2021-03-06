﻿using Lench.AdvancedControls.UI;
using UnityEngine;
// ReSharper disable CompareOfFloatsByEqualityOperator

namespace Lench.AdvancedControls.Axes
{
    /// <summary>
    /// Mouse axis is a screen-size independent mouse position axis.
    /// </summary>
    public class MouseAxis : InputAxis
    {
        /// <summary>
        /// Horizontal (X) or vertical (Y) axis.
        /// </summary>
        public Axis Axis { get; set; }

        /// <summary>
        /// Position on the screen where axis returns zero.
        /// 0 is center, -1 is left edge and +1 is right edge.
        /// </summary>
        public float Center
        {
            get { return _center; }
            set
            {
                _center = Mathf.Clamp(value, -1, 1);
            }
        }
        private float _center;

        /// <summary>
        /// Size of the range between -1 and +1 axis values.
        /// 1 for full screen and 0 for one pixel.
        /// </summary>
        public float Range
        {
            get { return _range; }
            set
            {
                _range = Mathf.Clamp(value, 0, 1);
            }
        }
        private float _range;

        /// <summary>
        /// Creates a new mouse axis with given name.
        /// </summary>
        /// <param name="name">Name of the axis.</param>
        public MouseAxis(string name) : base(name)
        {
            Type = AxisType.Mouse;
            Center = 0;
            Range = 0.5f;
            Axis = Axis.X;

            Editor = new MouseAxisEditor(this);
        }

        /// <summary>
        /// Mouse axis output value.
        /// </summary>
        public override float OutputValue
        {
            get
            {
                float mousePos = Axis == Axis.X ? UnityEngine.Input.mousePosition.x : UnityEngine.Input.mousePosition.y;
                float screenSize = Axis == Axis.X ? Screen.width : Screen.height;
                float rangeSize = Range == 0 ? 1 : screenSize * Range / 2f;
                float center = screenSize/2f + screenSize/2f * Center;
                return Mathf.Clamp((mousePos - center) / rangeSize, -1f, 1f);
            }
        }

        /// <summary>
        /// Compares axis tuning parameters.
        /// </summary>
        /// <param name="other">Axis to compare with.</param>
        /// <returns>Returns true if axes are identical.</returns>
        public override bool Equals(InputAxis other)
        {
            var cast = other as MouseAxis;
            if (cast == null) return false;
            return Name == cast.Name &&
                   Axis == cast.Axis &&
                   Center == cast.Center &&
                   Range == cast.Range;
        }

        /// <summary>
        /// Mouse axis requires no initialisation.
        /// </summary>
        protected override void Initialise() { }

        /// <summary>
        /// Mouse axis requires no update.
        /// </summary>
        protected override void Update(){ }

        internal override InputAxis Clone()
        {
            var clone = new MouseAxis(Name)
            {
                Axis = Axis,
                Center = Center,
                Range = Range
            };
            return clone;
        }

        internal override void Delete()
        {
            spaar.ModLoader.Configuration.RemoveKey("axis-" + Name + "-type");
            spaar.ModLoader.Configuration.RemoveKey("axis-" + Name + "-axis");
            spaar.ModLoader.Configuration.RemoveKey("axis-" + Name + "-center");
            spaar.ModLoader.Configuration.RemoveKey("axis-" + Name + "-range");
            Dispose();
        }

        internal override void Load()
        {
            Axis = (Axis)spaar.ModLoader.Configuration.GetInt("axis-" + Name + "-axis", (int)Axis);
            Center = spaar.ModLoader.Configuration.GetFloat("axis-" + Name + "-center", Center);
            Range = spaar.ModLoader.Configuration.GetFloat("axis-" + Name + "-range", Range);
        }

        internal override void Load(MachineInfo machineInfo)
        {
            if (machineInfo.MachineData.HasKey("axis-" + Name + "-axis"))
                Axis = (Axis)machineInfo.MachineData.ReadInt("axis-" + Name + "-axis");
            if (machineInfo.MachineData.HasKey("axis-" + Name + "-center"))
                Center = machineInfo.MachineData.ReadFloat("axis-" + Name + "-center");
            if (machineInfo.MachineData.HasKey("axis-" + Name + "-range"))
                Range = machineInfo.MachineData.ReadFloat("axis-" + Name + "-range");
        }

        internal override void Save()
        {
            spaar.ModLoader.Configuration.SetString("axis-" + Name + "-type", Type.ToString());
            spaar.ModLoader.Configuration.SetInt("axis-" + Name + "-axis", (int)Axis);
            spaar.ModLoader.Configuration.SetFloat("axis-" + Name + "-center", Center);
            spaar.ModLoader.Configuration.SetFloat("axis-" + Name + "-range", Range);
        }

        internal override void Save(MachineInfo machineInfo)
        {
            machineInfo.MachineData.Write("axis-" + Name + "-type", Type.ToString());
            machineInfo.MachineData.Write("axis-" + Name + "-axis", (int)Axis);
            machineInfo.MachineData.Write("axis-" + Name + "-center", Center);
            machineInfo.MachineData.Write("axis-" + Name + "-range", Range);
        }
    }
}
