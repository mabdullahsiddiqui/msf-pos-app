using System;

namespace pos_app.Client.Services
{
    public static class AccountSelectionState
    {
        public static string? ActiveModalSourceId { get; set; }
        
        public static event Action<string, int>? OnFromAccountSelected;
        public static event Action<string, int>? OnUptoAccountSelected;

        public static void SelectFrom(string code, int level)
        {
            OnFromAccountSelected?.Invoke(code, level);
        }

        public static void SelectUpto(string code, int level)
        {
            OnUptoAccountSelected?.Invoke(code, level);
        }
    }
}
