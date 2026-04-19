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
    }
}
