using System;
using Cirrious.MvvmCross.Droid.Views;
using Cirrious.MvvmCross.ViewModels;
using Cirrious.CrossCore;
using Android.Content;

namespace MvvmCross.Conditions.Droid
{
    public class ConditionalDroidViewsContainer: MvxAndroidViewsContainer
    {
        public ConditionalDroidViewsContainer(Context applicationContext) : base(applicationContext)
        {

        }

        protected override bool TryGetEmbeddedViewModel(Android.Content.Intent intent, out IMvxViewModel mvxViewModel)
        {
            var embeddedViewModelKey = intent.Extras.GetInt("MvxSubViewModelKey");
            if (embeddedViewModelKey != 0)
            {
                {
                    try {
                        mvxViewModel = Mvx.Resolve<IMvxChildViewModelCache>().Get(embeddedViewModelKey);
                    }
                    catch (Exception ex) {
                        mvxViewModel = null;
                        return false;                        
                    }
                    return true;
                }
            }
            mvxViewModel = null;
            return false;
        }
    }
}

