// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using GameController;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Input
{
    static partial class GamePad
    {
        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        private static extern float objc_msgSend_float(IntPtr receiver, IntPtr selector);

        /// <summary>
        /// Reads thumbstick and trigger analog values from an ExtendedGamepad.
        /// </summary>
        private static void ReadThumbsticks(GCExtendedGamepad gamepad,
            out float lx, out float ly, out float rx, out float ry,
            out float lt, out float rt)
        {
            lx = (float)gamepad.LeftThumbstick.XAxis.Value;
            ly = (float)gamepad.LeftThumbstick.YAxis.Value;
            rx = (float)gamepad.RightThumbstick.XAxis.Value;
            ry = (float)gamepad.RightThumbstick.YAxis.Value;
            lt = (float)gamepad.LeftTrigger.Value;
            rt = (float)gamepad.RightTrigger.Value;
        }

        internal static bool MenuPressed = false;
        // Thumbstick/trigger values stored from the last ExtendedGamepad read.
        // On tvOS, local Vector2 variables get mysteriously zeroed between the
        // foreach loop body and post-loop code, so we use static fields instead.
        private static float _lastLX, _lastLY, _lastRX, _lastRY, _lastLT, _lastRT;

        // .NET tvOS bindings throw InvalidCastException when accessing Gamepad/MicroGamepad
        // on controllers that don't have those profiles. These helpers catch the exception.
        private static bool SafeHasGamepad(GCController c)
        {
            try { return c.Gamepad != null; } catch { return false; }
        }
        private static bool SafeHasMicroGamepad(GCController c)
        {
            try { return c.MicroGamepad != null; } catch { return false; }
        }

        private static int PlatformGetMaxNumberOfGamePads()
        {
            return 4;
        }

        static bool IndexIsUsed(GCControllerPlayerIndex index)
        {
            foreach (var ctrl in GCController.Controllers)
                if (ctrl.PlayerIndex == index) return true;

            return false;
        }

        static void AssignIndex(GCControllerPlayerIndex index)
        {
            if (IndexIsUsed(index))
                return;
            foreach (var controller in GCController.Controllers)
            {
                if (controller.PlayerIndex == index)
                    break;
                if (controller.PlayerIndex == GCControllerPlayerIndex.Unset)
                {
                    controller.PlayerIndex = index;
                    break;
                }
            }
        }

        private static GamePadCapabilities PlatformGetCapabilities(int index)
        {
            var ind = (GCControllerPlayerIndex)index;

            AssignIndex(ind);

            foreach (var controller in GCController.Controllers)
            {
                if (controller == null)
                    continue;
                if (controller.PlayerIndex == ind)
                    return GetCapabilities(controller);
            }
            return new GamePadCapabilities { IsConnected = false };
        }

        private static GamePadCapabilities GetCapabilities(GCController controller)
        {
            //All iOS controllers have these basics
            var capabilities = new GamePadCapabilities()
            {
                IsConnected = false,
                GamePadType = GamePadType.GamePad,
            };
            if (controller.ExtendedGamepad != null)
            {
                capabilities.IsConnected = true;
                capabilities.HasAButton = true;
                capabilities.HasBButton = true;
                capabilities.HasXButton = true;
                capabilities.HasYButton = true;
                capabilities.HasBackButton = true;
                capabilities.HasStartButton = true;
                capabilities.HasDPadUpButton = true;
                capabilities.HasDPadDownButton = true;
                capabilities.HasDPadLeftButton = true;
                capabilities.HasDPadRightButton = true;
                capabilities.HasLeftShoulderButton = true;
                capabilities.HasRightShoulderButton = true;
                capabilities.HasLeftTrigger = true;
                capabilities.HasRightTrigger = true;
                capabilities.HasLeftXThumbStick = true;
                capabilities.HasLeftYThumbStick = true;
                capabilities.HasLeftStickButton = true;
                capabilities.HasRightXThumbStick = true;
                capabilities.HasRightYThumbStick = true;
                capabilities.HasRightStickButton = true;
            }
            else if (SafeHasGamepad(controller))
            {
                capabilities.IsConnected = true;
                capabilities.HasAButton = true;
                capabilities.HasBButton = true;
                capabilities.HasXButton = true;
                capabilities.HasYButton = true;
                capabilities.HasDPadUpButton = true;
                capabilities.HasDPadDownButton = true;
                capabilities.HasDPadLeftButton = true;
                capabilities.HasDPadRightButton = true;
                capabilities.HasLeftShoulderButton = true;
                capabilities.HasRightShoulderButton = true;
            }
            else if (SafeHasMicroGamepad(controller))
            {
                capabilities.IsConnected = true;
                capabilities.HasAButton = true;
                capabilities.HasXButton = true;
                capabilities.HasStartButton = true;
                capabilities.HasDPadUpButton = true;
                capabilities.HasDPadDownButton = true;
                capabilities.HasDPadLeftButton = true;
                capabilities.HasDPadRightButton = true;
                capabilities.HasLeftXThumbStick = true;
                capabilities.HasLeftYThumbStick = true;
            }
            return capabilities;
        }

        private static GamePadState PlatformGetState(int index, GamePadDeadZone leftDeadZoneMode, GamePadDeadZone rightDeadZoneMode)
        {
            Buttons buttons = 0;
            bool connected = false;
            ButtonState Up = ButtonState.Released;
            ButtonState Down = ButtonState.Released;
            ButtonState Left = ButtonState.Released;
            ButtonState Right = ButtonState.Released;

            Vector2 leftThumbStickPosition = Vector2.Zero;
            Vector2 rightThumbStickPosition = Vector2.Zero;

            float leftTriggerValue = 0;
            float rightTriggerValue = 0;

            foreach (var controller in GCController.Controllers)
            {
                if (controller == null)
                    continue;

                // validate controller has a valid input profile before reporting as connected
                bool hasProfile = controller.ExtendedGamepad != null;
                if (!hasProfile) { try { hasProfile = controller.MicroGamepad != null; } catch { } }
                if (!hasProfile) { try { hasProfile = controller.Gamepad != null; } catch { } }
                if (!hasProfile)
                    continue;

                // For index 0, accept any controller (prefer ExtendedGamepad)
                // For other indices, try to match by player index
                if (index > 0)
                {
                    try
                    {
                        var ind = (GCControllerPlayerIndex)index;
                        if (controller.PlayerIndex != ind)
                            continue;
                    }
                    catch { continue; }
                }

                connected = true;

#if TVOS
                // Siri Remote — MicroGamepad profile (touchpad + A/X + Menu)
                if (SafeHasMicroGamepad(controller) && controller.ExtendedGamepad == null)
                {
                    if (controller.MicroGamepad.ButtonA.IsPressed)
                        buttons |= Buttons.A;
                    if (controller.MicroGamepad.ButtonX.IsPressed)
                        buttons |= Buttons.X;

                    if (controller.MicroGamepad.ButtonMenu?.IsPressed == true)
                        buttons |= Buttons.Start;

                    // Map the touchpad directional input to DPad
                    if (controller.MicroGamepad.Dpad.Up.IsPressed)
                    {
                        Up = ButtonState.Pressed;
                        buttons |= Buttons.DPadUp;
                    }
                    if (controller.MicroGamepad.Dpad.Down.IsPressed)
                    {
                        Down = ButtonState.Pressed;
                        buttons |= Buttons.DPadDown;
                    }
                    if (controller.MicroGamepad.Dpad.Left.IsPressed)
                    {
                        Left = ButtonState.Pressed;
                        buttons |= Buttons.DPadLeft;
                    }
                    if (controller.MicroGamepad.Dpad.Right.IsPressed)
                    {
                        Right = ButtonState.Pressed;
                        buttons |= Buttons.DPadRight;
                    }

                    // Map touchpad position to left thumbstick
                    leftThumbStickPosition = new Vector2(
                        controller.MicroGamepad.Dpad.XAxis.Value,
                        controller.MicroGamepad.Dpad.YAxis.Value);

                    if (MenuPressed)
                    {
                        buttons |= Buttons.Back;
                        MenuPressed = false;
                    }
                }
                else
#endif
                if (controller.ExtendedGamepad != null)
                {
                    if (controller.ExtendedGamepad.ButtonA.IsPressed)
                        buttons |= Buttons.A;
                    if (controller.ExtendedGamepad.ButtonB.IsPressed)
                        buttons |= Buttons.B;
                    if (controller.ExtendedGamepad.ButtonX.IsPressed)
                        buttons |= Buttons.X;
                    if (controller.ExtendedGamepad.ButtonY.IsPressed)
                        buttons |= Buttons.Y;

                    if (controller.ExtendedGamepad.LeftShoulder.IsPressed)
                        buttons |= Buttons.LeftShoulder;
                    if (controller.ExtendedGamepad.RightShoulder.IsPressed)
                        buttons |= Buttons.RightShoulder;

                    if (controller.ExtendedGamepad.LeftTrigger.IsPressed)
                        buttons |= Buttons.LeftTrigger;
                    if (controller.ExtendedGamepad.RightTrigger.IsPressed)
                        buttons |= Buttons.RightTrigger;

                    if (controller.ExtendedGamepad.ButtonMenu != null
                    && controller.ExtendedGamepad.ButtonMenu.IsPressed)
                    {
                        buttons |= Buttons.Start;
                    }
                        
                    if (controller.ExtendedGamepad.ButtonOptions?.IsPressed == true)
                    {
                        buttons |= Buttons.Back;
                    }

                    if (controller.ExtendedGamepad.DPad.Up.IsPressed)
                    {
                        Up = ButtonState.Pressed;
                        buttons |= Buttons.DPadUp;
                    }
                    if (controller.ExtendedGamepad.DPad.Down.IsPressed)
                    {
                        Down = ButtonState.Pressed;
                        buttons |= Buttons.DPadDown;
                    }
                    if (controller.ExtendedGamepad.DPad.Left.IsPressed)
                    {
                        Left = ButtonState.Pressed;
                        buttons |= Buttons.DPadLeft;
                    }
                    if (controller.ExtendedGamepad.DPad.Right.IsPressed)
                    {
                        Right = ButtonState.Pressed;
                        buttons |= Buttons.DPadRight;
                    }

                    if (controller.ExtendedGamepad.LeftThumbstickButton != null
                    && controller.ExtendedGamepad.LeftThumbstickButton.IsPressed)
                    {
                        buttons |= Buttons.LeftStick;
                    }

                    if (controller.ExtendedGamepad.RightThumbstickButton != null
                    && controller.ExtendedGamepad.RightThumbstickButton.IsPressed)
                    {
                        buttons |= Buttons.RightStick;
                    }

                    // Read thumbstick/trigger values via raw ObjC runtime
                    // .NET tvOS bindings throw InvalidCastException on some properties
                    ReadThumbsticks(controller.ExtendedGamepad,
                        out _lastLX, out _lastLY, out _lastRX, out _lastRY,
                        out _lastLT, out _lastRT);
                }
                else if (SafeHasGamepad(controller))
                {
                    if (controller.Gamepad.ButtonA.IsPressed)
                        buttons |= Buttons.A;
                    if (controller.Gamepad.ButtonB.IsPressed)
                        buttons |= Buttons.B;
                    if (controller.Gamepad.ButtonX.IsPressed)
                        buttons |= Buttons.X;
                    if (controller.Gamepad.ButtonY.IsPressed)
                        buttons |= Buttons.Y;
                    if (controller.Gamepad.LeftShoulder.IsPressed)
                        buttons |= Buttons.LeftShoulder;
                    if (controller.Gamepad.RightShoulder.IsPressed)
                        buttons |= Buttons.RightShoulder;

                    if (controller.Gamepad.DPad.Up.IsPressed)
                    {
                        Up = ButtonState.Pressed;
                        buttons |= Buttons.DPadUp;
                    }
                    if (controller.Gamepad.DPad.Down.IsPressed)
                    {
                        Down = ButtonState.Pressed;
                        buttons |= Buttons.DPadDown;
                    }
                    if (controller.Gamepad.DPad.Left.IsPressed)
                    {
                        Left = ButtonState.Pressed;
                        buttons |= Buttons.DPadLeft;
                    }
                    if (controller.Gamepad.DPad.Right.IsPressed)
                    {
                        Right = ButtonState.Pressed;
                        buttons |= Buttons.DPadRight;
                    }
                }
            }
            var state = new GamePadState(
                new GamePadThumbSticks(new Vector2(_lastLX, _lastLY), new Vector2(_lastRX, _lastRY), leftDeadZoneMode, rightDeadZoneMode),
                new GamePadTriggers(_lastLT, _lastRT),
                new GamePadButtons(buttons),
                new GamePadDPad(Up, Down, Left, Right));
            state.IsConnected = connected;
            return state;
        }

        private static bool PlatformSetVibration(int index, float leftMotor, float rightMotor, float leftTrigger, float rightTrigger)
        {
            return false;
        }
    }
}
