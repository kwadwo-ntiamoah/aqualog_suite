using Google.Cloud.Firestore;

namespace API.Infrastructure.Persistence
{
    public static class FirestoreConfig
    {
        public static IServiceCollection AddFirestore(this IServiceCollection services, IConfiguration config)
        {
            services.AddSingleton(_ => BuildFirestoreDb(config));

            return services;
        }

        // No default is baked in here, same discipline as the rest of this
        // app's config — Firestore:ProjectId and Firestore:CredentialsJson
        // must be set explicitly (via Render env vars in production; see
        // render.yaml) rather than falling back to a silently-wrong value.
        private static FirestoreDb BuildFirestoreDb(IConfiguration config)
        {
            var projectId = config.GetValue<string>("Firestore:ProjectId");
            var credentialsJson = config.GetValue<string>("Firestore:CredentialsJson");
            // A GCP project can hold multiple named Firestore databases —
            // the client defaults to looking for one literally named
            // "(default)", which only matches if that's what you named it
            // when creating it in the console. Override via Firestore:DatabaseId
            // if yours has a different name.
            var databaseId = config.GetValue<string>("Firestore:DatabaseId") ?? "(default)";

            if (string.IsNullOrWhiteSpace(projectId) || string.IsNullOrWhiteSpace(credentialsJson))
            {
                throw new InvalidOperationException("Firestore:ProjectId and Firestore:CredentialsJson must be set.");
            }

            return new FirestoreDbBuilder
            {
                ProjectId = projectId,
                DatabaseId = databaseId,
                JsonCredentials = credentialsJson,
            }.Build();
        }
    }
}
