using System;

namespace HRS.Services
{
    public class EventBus
    {
        private static EventBus _instance;
        public static EventBus Instance => _instance ?? (_instance = new EventBus());

        public event Action DataChanged;
        public event Action ReservationUpdated;
        public event Action RoomUpdated;
        public event Action NavigateToReservationsRequested;
        public event Action NewReservationRequested;
        public event Action<string> NavigateToCustomerRequested;
        public event Action<string> NavigateToRoomRequested;

        public void PublishDataChanged()
        {
            DataChanged?.Invoke();
        }

        public void PublishReservationUpdated()
        {
            ReservationUpdated?.Invoke();
            PublishDataChanged();
        }

        public void PublishRoomUpdated()
        {
            RoomUpdated?.Invoke();
            PublishDataChanged();
        }

        public void PublishNavigateToReservations()
        {
            NavigateToReservationsRequested?.Invoke();
        }

        public void PublishNewReservation()
        {
            NewReservationRequested?.Invoke();
        }

        public void PublishNavigateToCustomer(string customerId)
        {
            NavigateToCustomerRequested?.Invoke(customerId);
        }

        public void PublishNavigateToRoom(string roomId)
        {
            NavigateToRoomRequested?.Invoke(roomId);
        }
    }
}
