using System;
using Android.Content;
using System.Collections.Generic;

namespace MvvmCross.Conditions.Droid
{
    public interface IManipulateIntentService
    {
        Intent ManipulateIntent(Intent inIntent, string className);

        void AddFlag(string className, ActivityFlags flag);

        void RemoveFlag(string className, ActivityFlags flag);

        void SetFlags(string className, List<ActivityFlags> flags);

        void ClearFlags(string className);
    }
}

