using System;

namespace HostelManagementSystem.Messages
{
    public class DatabaseUpdatedMessage
    {
        // What type of entity was changed? (e.g., "Student", "Room", "Booking")
        public string EntityName { get; }

        // The ID of the specific record that was changed
        public int EntityId { get; }

        // What kind of operation happened?
        public DatabaseOperation Operation { get; }

        // When did the update occur?
        public DateTime Timestamp { get; }

        public DatabaseUpdatedMessage(string entityName, int entityId, DatabaseOperation operation)
        {
            EntityName = entityName;
            EntityId = entityId;
            Operation = operation;
            Timestamp = DateTime.UtcNow;
        }
    }

    // An enum to define the type of change
    public enum DatabaseOperation
    {
        Inserted,
        Updated,
        Deleted
    }
}