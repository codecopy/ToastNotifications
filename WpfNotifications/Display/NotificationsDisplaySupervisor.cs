﻿using System;
using System.Windows;
using System.Windows.Threading;
using WpfNotifications.Core;
using WpfNotifications.Lifetime;
using WpfNotifications.Utilities;

namespace WpfNotifications.Display
{
    public class NotificationsDisplaySupervisor : IDisposable
    {
        private readonly object _syncRoot = new object();
        private readonly Dispatcher _dispatcher;
        private readonly IPositionProvider _positionProvider;
        private INotificationsLifetimeSupervisor _lifetimeSupervisor;
        private NotificationsWindow _window;

        public NotificationsDisplaySupervisor(Dispatcher dispatcher, 
            IPositionProvider positionProvider, 
            INotificationsLifetimeSupervisor lifetimeSupervisor)
        {
            _dispatcher = dispatcher;
            _positionProvider = positionProvider;
            _lifetimeSupervisor = lifetimeSupervisor;

            _lifetimeSupervisor.ShowNotificationRequested += LifetimeSupervisorOnShowNotificationRequested;
            _lifetimeSupervisor.CloseNotificationRequested += LifetimeSupervisorOnCloseNotificationRequested;

            _positionProvider.UpdatePositionRequested += PositionProviderOnUpdatePositionRequested;
        }

        public void DisplayNotification(INotification notification)
        {
            Dispatch(() => InternalDisplayNotification(notification));
        }

        private void InternalDisplayNotification(INotification notification)
        {
            InitializeWindow();

            ShowNotification(notification);
            ShowWindow();
            UpdateEjectDirection();
            UpdateWindowPosition();
        }

        private void Close(INotification notification)
        {
            Dispatch(() => InternalClose(notification));
        }

        private void InternalClose(INotification notification)
        {
            _lifetimeSupervisor.CloseNotification(notification);
            UpdateWindowPosition();
        }

        private void Dispatch(Action action)
        {
            _dispatcher.Invoke(action);
        }

        private void InitializeWindow()
        {
            lock (_syncRoot)
            {
                if (_window != null)
                    return;

                _window = new NotificationsWindow(_positionProvider.ParentWindow);
                _window.MinHeight = _positionProvider.GetHeight();
                _window.Height = _positionProvider.GetHeight();
                _window.SetPosition(new Point(Double.NaN, Double.NaN));
            }
        }

        private void UpdateWindowPosition()
        {
            Point position = _positionProvider.GetPosition(_window.GetWidth(), _window.GetHeight());
            _window.SetPosition(position);
        }

        private void UpdateEjectDirection()
        {
            _window.SetEjectDirection(_positionProvider.EjectDirection);
        }

        private void ShowNotification(INotification notification)
        {
            notification.Bind(Close);
            _window.ShowNotification(notification.DisplayPart);
        }

        private void CloseNotification(INotification notification)
        {
            notification.DisplayPart.OnClose();
            DelayAction.Execute(TimeSpan.FromMilliseconds(300), () => _window.CloseNotification(notification.DisplayPart));
        }

        private void ShowWindow()
        {
            _window.Show();
        }

        private void LifetimeSupervisorOnShowNotificationRequested(object sender, ShowNotificationEventArgs eventArgs)
        {
            DisplayNotification(eventArgs.Notification);
        }

        private void LifetimeSupervisorOnCloseNotificationRequested(object sender, CloseNotificationEventArgs eventArgs)
        {
            CloseNotification(eventArgs.Notification);
        }

        private void PositionProviderOnUpdatePositionRequested(object sender, EventArgs eventArgs)
        {
            UpdateWindowPosition();
        }

        public void Dispose()
        {
            _window?.Close();
            _window = null;

            _lifetimeSupervisor.ShowNotificationRequested -= LifetimeSupervisorOnShowNotificationRequested;
            _lifetimeSupervisor.CloseNotificationRequested -= LifetimeSupervisorOnCloseNotificationRequested;

            _positionProvider.UpdatePositionRequested -= PositionProviderOnUpdatePositionRequested;

            _lifetimeSupervisor = null;
        }
    }
}
