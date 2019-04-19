using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;

namespace MayBot.Dialogs
{
    public class BaseDialogClass<T> : ComponentDialog where T: class, new()
    {
        private DialogContext dialogContext;

        public BaseDialogClass(string dialogId) : base(dialogId)
        {
        }

        protected T State { get; set; }

        public override Task<DialogTurnResult> BeginDialogAsync(DialogContext outerDc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            this.dialogContext = outerDc;
            InitializeState();
            return base.BeginDialogAsync(outerDc, options, cancellationToken);
        }

        public override Task<DialogTurnResult> ContinueDialogAsync(DialogContext outerDc, CancellationToken cancellationToken = default(CancellationToken))
        {
            this.dialogContext = outerDc;
            FetchState();
            return base.ContinueDialogAsync(outerDc, cancellationToken);
        }

        private void InitializeState()
        {
            this.State = new T();
            SaveState();
        }

        private void SaveState()
        {
            if (this.dialogContext.ActiveDialog != null)
            {
                this.dialogContext.ActiveDialog.State["ObjectStateKey"] = this.State ?? new T();
            }
        }

        private void FetchState()
        {
            if (this.dialogContext.ActiveDialog != null)
            {
                if (this.dialogContext.ActiveDialog.State.TryGetValue("ObjectStateKey", out object currentState))
                {
                    this.State = (T)currentState;
                }
                else
                {
                    this.State = new T();
                }
            }
        }
    }
}
