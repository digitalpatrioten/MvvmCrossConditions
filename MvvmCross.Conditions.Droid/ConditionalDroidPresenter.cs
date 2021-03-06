﻿using System;
using Cirrious.CrossCore.Exceptions;
using Cirrious.MvvmCross.ViewModels;
using Cirrious.CrossCore;
using MvvmCross.Conditions.Core;
using Cirrious.MvvmCross.Droid.Views;
using Cirrious.CrossCore.Droid.Platform;
using Android.App;
using Android.OS;
using Android.Util;
using System.Threading.Tasks;

namespace MvvmCross.Conditions.Droid
{
    public class ViewNotAllowedException : Exception
    {

    }

    public delegate void ViewModelLoadedEventHandler(MvxViewModelRequest request,IConditionalViewModel viewModel,bool viewModelShouldHandleError);

    public class ConditionalDroidPresenter : MvxAndroidViewPresenter
    {
      
        public ConditionalDroidPresenter() : base()
        {
            ViewModelLoaded += HandleViewModelLoaded;
        }

        void HandleViewModelLoaded(MvxViewModelRequest request, IConditionalViewModel viewModel, bool viewModelShouldHandleError)
        {
            //we need to run the check on the main thread so view models can have some sort of visual output if desired
            Mvx.Resolve<IMvxAndroidCurrentTopActivity>().Activity.RunOnUiThread(() => {
                if (viewModel.Precondition(viewModelShouldHandleError) == false) {
                    if (viewModelShouldHandleError) {
                        // in this case the ViewModel already handled the error - if it did nothing ( no redirect ) basically just nothing happens
                        return;
                    }
                    else {
                        // The calle asked us ot inform him if the viewmodel does not allow instantiation to handle the error himself, so throw an Exception
                        throw new ViewNotAllowedException();
                    }
                }
                else {
                    //for us this does not work right now and leads to strange errors, not finding the view model after tombstoning
                    var cacheKey = Mvx.Resolve<IMvxChildViewModelCache>().Cache(viewModel);
                    ShowViewController(request, cacheKey);
                }
            });
        }

        public override void Show(MvxViewModelRequest request)
        {
            Show(request, true);
        }

        protected event ViewModelLoadedEventHandler ViewModelLoaded;

        protected virtual void OnViewModelLoaded(MvxViewModelRequest request, IConditionalViewModel viewModel, bool viewModelShouldHandleError)
        {
            var handler = ViewModelLoaded;
            if (handler != null)
                handler(request, viewModel, viewModelShouldHandleError);
        }

        // thats an OVERLOAD!, just in case somebody reads way to fast ;)
        public void Show(MvxViewModelRequest request, bool viewModelShouldHandleError = true)
        {
            // TODO: use an as cast with an null check instead?
            if (ImplementsInterface(request.ViewModelType, typeof(IConditionalViewModel))) {
                // check condition here
                var loaderTask = new Task(() => {
                    var loader = Mvx.Resolve<IMvxViewModelLoader>();
                    var viewModel = loader.LoadViewModel(request, null) as IConditionalViewModel;
                    OnViewModelLoaded(request, viewModel, viewModelShouldHandleError);
                });

                //we need to start the model load async so we dont block the uithread
                loaderTask.Start();
            }
            else {
                ShowViewController(request);
            }
        }

        protected void ShowViewController(MvxViewModelRequest request, int cacheKey = -1)
        {
            ViewDetails viewDetails = IdentifyView(request);

            if (viewDetails.category == ViewCategory.Fragment) {

                ShowFragment(request, cacheKey);
            }
            else {

                ShowActivity(request, cacheKey);
            }
        }

        protected void ShowActivity(MvxViewModelRequest request, int cacheKey = -1)
        {
            var intent = CreateIntentForRequest(request);

            if (cacheKey >= 0) {// reuse the viewModel

                // This little thingy is used to "inject the viewmodel" into the activity to avoid reinstantiation
                // see MvxAndroidViewsContainer.TryGetEmbeddedViewModel
                Bundle extras = intent.Extras;
                extras.PutInt("MvxSubViewModelKey", cacheKey); // MvxSubViewModelKey is a special static key.. we cant use MvxAndroidViewsContainer.SubViewModelKey since its static 
                intent.PutExtras(extras);
            }

            var className = intent.ResolveActivity(this.Activity.PackageManager).ClassName;

            try {
                var intentService = Mvx.Resolve<IManipulateIntentService>();
                if (intentService != null) {
                    intent = intentService.ManipulateIntent(intent, className);
                }
            }
            catch (Exception e) {
                Log.WriteLine(LogPriority.Error, "ShowActivity", e.Message);
            }

            Show(intent);
        }

        protected void ShowFragment(MvxViewModelRequest request, int cacheKey = -1)
        {
            throw new NotImplementedException();
            // TODO: this needs to be implemented fro fragment-navigation support
            /*
            public interface IMvxChildViewContainer
            {
                Fragment RootFragment {
                    get;
                }
            }


            var activity = Mvx.Resolve<IMvxAndroidCurrentTopActivity>().Activity;
            IMvxChildViewContainer fragmentActivity = activity as IMvxChildViewContainer;

            if (fragmentActivity != null) {
                Fragment rootFragment = (Fragment)fragmentActivity.RootFragment;
                IMvxParameterValuesContainer newFragment = (IMvxParameterValuesContainer)Activator.CreateInstance(viewDetails.type, fragmentActivity);
                newFragment.ParameterValues = request.ParameterValues;
                FragmentTransaction trans = rootFragment.FragmentManager.BeginTransaction();
                IMvxViewGroupContainer rootFragment = rootFragment as IMvxViewGroupContainer;
                if (rootFragment != null) {
                    Fragment fragmentToReplace = newFragment as Fragment;
                    trans.Replace(rootFragment.Layout.Id, fragmentToReplace);
                    trans.AddToBackStack(null);
                    trans.Commit();

                    IMvxParameterValuesStackContainer stackContainer = rootFragment as IMvxParameterValuesStackContainer;
                    if (stackContainer != null) {
                    }
                }
            }
            else {
                // TODO: throw an exception that the fragment is not able to be shown
                // since the activity is incompatible
            }*/
        }

        public bool ImplementsInterface(Type type, Type ifaceType)
        {
            Type[] intf = type.GetInterfaces();
            for (int i = 0; i < intf.Length; i++) {
                if (intf[i] == ifaceType) {
                    return true;
                }
            }
            return false;
        }
    }
}

