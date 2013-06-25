// (c) Microsoft. All rights reserved
using System;
using System.Threading;
using System.Threading.Tasks;
using HealthVault.Foundation.Methods;
using HealthVault.Foundation.Types;

namespace HealthVault.Foundation
{
    public class ServiceMethods
    {
        private readonly HealthVaultClient m_client;

        public ServiceMethods(HealthVaultClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException("client");
            }

            m_client = client;
        }

        public async Task<AppProvisioningInfo> GetAppProvisioningInfoAsync(CancellationToken cancelToken)
        {
            var method = new NewApplicationProvisioningInfo(m_client);
            Response response = await method.ExecuteAsync(cancelToken);
            return (AppProvisioningInfo) response.GetResult();
        }

        public async Task<SessionCredential> GetSessionTokenAsync(CancellationToken cancelToken)
        {
            var method = new CreateAuthenticatedSessionToken(m_client);
            Response response = await method.ExecuteAsync(cancelToken);
            return (SessionCredential) response.GetResult();
        }

        public async Task<PersonInfo[]> GetAuthorizedPersonsAsync(CancellationToken cancelToken)
        {
            var method = new GetAuthorizedPeople(m_client);
            Response response = await method.ExecuteAsync(cancelToken);
            var authPeopleResponse = (GetAuthorizedPeopleResponse) response.GetResult();
            return authPeopleResponse.HasResults ? authPeopleResponse.Results.Persons : null;
        }

        public async Task<TResult> GetThingType<TResult>(object getThingTypeParams, CancellationToken cancelToken)
        {
            if (getThingTypeParams == null)
            {
                throw new ArgumentNullException("getThingTypeParams");
            }

            var body = new RequestBody(getThingTypeParams);
            var method = new GetThingType(m_client, body, typeof (TResult));
            Response response = await method.ExecuteAsync();

            return (TResult) response.GetResult();
        }

        public async Task<TResult> GetVocabularies<TResult>(object getVocabParams, CancellationToken cancelToken)
        {
            if (getVocabParams == null)
            {
                throw new ArgumentNullException("getVocabParams");
            }

            var body = new RequestBody(getVocabParams);
            var method = new GetVocabulary(m_client, body, typeof (TResult));
            Response response = await method.ExecuteAsync();

            return (TResult) response.GetResult();
        }

        public async Task<TResult> SearchVocabulary<TResult>(object searchVocabParams, CancellationToken cancelToken)
        {
            if (searchVocabParams == null)
            {
                throw new ArgumentNullException("getVocabParams");
            }

            var body = new RequestBody(searchVocabParams);
            var method = new SearchVocabulary(m_client, body, typeof (TResult));
            Response response = await method.ExecuteAsync();

            return (TResult) response.GetResult();
        }
    }
}