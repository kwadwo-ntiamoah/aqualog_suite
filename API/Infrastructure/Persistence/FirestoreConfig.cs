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

            if (string.IsNullOrWhiteSpace(projectId) || string.IsNullOrWhiteSpace(credentialsJson))
            {
                throw new InvalidOperationException("Firestore:ProjectId and Firestore:CredentialsJson must be set.");
            }

            return new FirestoreDbBuilder
            {
                ProjectId = projectId,
                JsonCredentials = credentialsJson,
            }.Build();
        }
    }
}
