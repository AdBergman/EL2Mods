using UnityEngine;

namespace EL2.BulkTrade
{
    internal static class BulkTradeInput
    {
        public static int GetTradeQuantity()
        {
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (ctrl)
                return 50;

            if (shift)
                return 10;

            return 1;
        }
    }
}