﻿using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;

namespace WPFTabTip
{
    public static class TabTipAutomation
    {
        static TabTipAutomation()
        {
            TabTip.Closed += () => FocusSubject.OnNext(new Tuple<UIElement, bool>(null, false));

            AutomateTabTipOpen(FocusSubject.AsObservable());
            AutomateTabTipClose(FocusSubject.AsObservable());
        }

        private static readonly Subject<Tuple<UIElement, bool>> FocusSubject = new Subject<Tuple<UIElement, bool>>(); 

        private static readonly List<Type> BindedUIElements = new List<Type>();

        /// <summary>
        /// TabTip automation happens only when no keyboard is connected to device.
        /// Set IgnoreHardwareKeyboard to true if you want to automate
        /// TabTip even if keyboard is connected.
        /// </summary>
        public static bool IgnoreHardwareKeyboard { get; set; }

        /// <summary>
        /// Description of keyboards to ignore if there is only one instance of given keyboard.
        /// If you want to ignore some ghost keyboard, add it's description to this list
        /// </summary>
        public static List<string> ListOfHardwareKeyboardsToIgnoreIfSingleInstance => HardwareKeyboard.IgnoreIfSingleInstance;

        private static void AutomateTabTipClose(IObservable<Tuple<UIElement, bool>> FocusObservable)
        {
            FocusObservable
                .ObserveOn(Scheduler.Default)
                .Where(_ => IgnoreHardwareKeyboard || !HardwareKeyboard.IsConnectedAsync().Result)
                .Throttle(TimeSpan.FromMilliseconds(100)) // Close only if no other UIElement got focus in 100 ms
                .Where(tuple => tuple.Item2 == false)
                .Do(_ => TabTip.Close())
                .ObserveOnDispatcher()
                .Subscribe(_ => AnimationHelper.GetEverythingInToWorkAreaWithTabTipClosed());
        }

        private static void AutomateTabTipOpen(IObservable<Tuple<UIElement, bool>> FocusObservable)
        {
            FocusObservable
                .ObserveOn(Scheduler.Default)
                .Where(_ => IgnoreHardwareKeyboard || !HardwareKeyboard.IsConnectedAsync().Result)
                .Where(tuple => tuple.Item2 == true)
                .Do(_ => TabTip.OpenUndockedAndStartPoolingForClosedEvent())
                .ObserveOnDispatcher()
                .Subscribe(tuple => AnimationHelper.GetUIElementInToWorkAreaWithTabTipOpened(tuple.Item1));
        }

        /// <summary>
        /// Automate TabTip for given UIElement.
        /// Keyboard opens and closes on GotFocusEvent and LostFocusEvent respectively.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void BindTo<T>() where T : UIElement
        {
            if (BindedUIElements.Contains(typeof(T)))
                return;

            EventManager.RegisterClassHandler(
                classType: typeof(T), 
                routedEvent: UIElement.GotFocusEvent, 
                handler: new RoutedEventHandler((s, e) => FocusSubject.OnNext(new Tuple<UIElement, bool>((UIElement) s, true))), 
                handledEventsToo: true);
            EventManager.RegisterClassHandler(
                classType: typeof(T), 
                routedEvent: UIElement.LostFocusEvent, 
                handler: new RoutedEventHandler((s, e) => FocusSubject.OnNext(new Tuple<UIElement, bool>((UIElement) s, false))), 
                handledEventsToo: true);

            BindedUIElements.Add(typeof(T));
        }
    }
}
