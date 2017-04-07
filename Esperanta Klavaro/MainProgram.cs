using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Esperanta_Klavaro
{
    public partial class MainProgram : ApplicationContext
    {
        KeyboardHook kbHook;
        NotifyIcon ni;
        ContextMenu cm;
        System.Drawing.Icon onIcon;
        System.Drawing.Icon offIcon;

        char lastKeyPressed;

        [STAThread]
        public static void Main()
        {
            Application.Run(new MainProgram());
        }


        public MainProgram()
        {
            onIcon = new System.Drawing.Icon("greenstar2.ico");
            offIcon = new System.Drawing.Icon("graystar.ico");

            cm = new ContextMenu();
            cm.MenuItems.Add("Malŝalti", new EventHandler(Toggle_OnClick));
            cm.MenuItems.Add("Fermi", new EventHandler(Close_onClick));
            
            ni = new NotifyIcon();
            ni.Icon = onIcon;
            ni.ContextMenu = cm;
            ni.Text = "Esperanta Klavaro";
            ni.Visible = true;

            KeyboardHook.Modifiers modifiers = KeyboardHook.Modifiers.MOD_CONTROL;

            Keys k = Keys.Space;
            
            kbHook = new KeyboardHook(modifiers, k);

            kbHook.KeyPressed += new KeyPressEventHandler(KeyPressHandler);

            kbHook.HotKeyPressed += new EventHandler(Toggle_OnClick);
        }

        private void Toggle_OnClick(object sender, EventArgs e)
        {
            if (kbHook.ToggleHook())
            {
                ni.Icon = onIcon;
                ni.ContextMenu.MenuItems[0].Text = "Malŝalti";
            }
            else
            {
                ni.Icon = offIcon;
                ni.ContextMenu.MenuItems[0].Text = "Enŝalti";
            }
        }

        private void Close_onClick(object sender, EventArgs e)
        {
            kbHook.UnregisterToggleHotKey();
            Application.Exit();
        }

        public void KeyPressHandler(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 'X' || e.KeyChar == 'x')
            {
                char accentedChar = GetAccented(lastKeyPressed);
                if (accentedChar != lastKeyPressed)
                {
                    SendKeys.SendWait("{BACKSPACE}" + accentedChar);
                    e.Handled = true;
                }
                else
                    lastKeyPressed = e.KeyChar;
            }
            else
            {
                lastKeyPressed = e.KeyChar;
            }
        }
       
        private char GetAccented(char c)
        {
            switch (c)
            {
                case 'c': return 'ĉ';
                case 'C': return 'Ĉ';
                case 'g': return 'ĝ';
                case 'G': return 'Ĝ';
                case 'j': return 'ĵ';
                case 'J': return 'Ĵ';
                case 's': return 'ŝ';
                case 'S': return 'Ŝ';
                case 'u': return 'ŭ';
                case 'U': return 'Ŭ';
                default: return c;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                ni.Dispose();
            base.Dispose(disposing);
        }
    }
}
