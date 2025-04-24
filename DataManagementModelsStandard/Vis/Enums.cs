using System;
using System.Collections.Generic;
using System.Text;

namespace TheTechIdea.Beep.Vis
{
    public enum DisplayType
    {
        Popup, InControl
    }
    [Flags]
    public enum EnumPointType
    {
        None = 0,
        Genre = 1 << 0,       // 1
        Root = 1 << 1,        // 2
        DataPoint = 1 << 2,   // 4
        Category = 1 << 3,    // 8
        Entity = 1 << 4,      // 16
        SubEntity = 1 << 5,   // 32
        Function = 1 << 6,    // 64
        Report = 1 << 7,      // 128
        Global = 1 << 8       // 256
    }

    [Flags]
    public enum ShowinType
    {
        None = 0,
        Menu = 1 << 0,        // 1
        Toolbar = 1 << 1,     // 2
        Both = Menu | Toolbar, // 3
        HorZToolbar = 1 << 2,  // 4
        ContextMenu = 1 << 3   // 8
    }

    public enum BeepKeys
    {
        None,
        Back,
        Tab,
        Clear,
        Enter,
        Shift,
        Control,
        Alt,
        Pause,
        CapsLock,
        Escape,
        Space,
        PageUp,
        PageDown,
        End,
        Home,
        Left,
        Up,
        Right,
        Down,
        Select,
        Print,
        Execute,
        PrintScreen,
        Insert,
        Delete,
        Help,
        D0,
        D1,
        D2,
        D3,
        D4,
        D5,
        D6,
        D7,
        D8,
        D9,
        A,
        B,
        C,
        D,
        E,
        F,
        G,
        H,
        I,
        J,
        K,
        L,
        M,
        N,
        O,
        P,
        Q,
        R,
        S,
        T,
        U,
        V,
        W,
        X,
        Y,
        Z,
        LeftWindows,
        RightWindows,
        Applications,
        Sleep,
        NumPad0,
        NumPad1,
        NumPad2,
        NumPad3,
        NumPad4,
        NumPad5,
        NumPad6,
        NumPad7,
        NumPad8,
        NumPad9,
        Multiply,
        Add,
        Separator,
        Subtract,
        Decimal,
        Divide,
        F1,
        F2,
        F3,
        F4,
        F5,
        F6,
        F7,
        F8,
        F9,
        F10,
        F11,
        F12,
        F13,
        F14,
        F15,
        F16,
        F17,
        F18,
        F19,
        F20,
        F21,
        F22,
        F23,
        F24,
        NumLock,
        Scroll,
        LeftShift,
        RightShift,
        LeftControl,
        RightControl,
        LeftAlt,
        RightAlt,
        BrowserBack,
        BrowserForward,
        BrowserRefresh,
        BrowserStop,
        BrowserSearch,
        BrowserFavorites,
        BrowserHome,
        VolumeMute,
        VolumeDown,
        VolumeUp,
        MediaNextTrack,
        MediaPreviousTrack,
        MediaStop,
        MediaPlayPause,
        LaunchMail,
        SelectMedia,
        LaunchApplication1,
        LaunchApplication2,
        Oem1,
        OemPlus,
        OemComma,
        OemMinus,
        OemPeriod,
        Oem2,
        Oem3,
        Oem4,
        Oem5,
        Oem6,
        Oem7,
        Oem8,
        Oem102,
        ProcessKey,
        Packet,
        Attn,
        Crsel,
        Exsel,
        EraseEof,
        Play,
        Zoom,
        NoName,
        Pa1,
        OemClear,
    }

}
