/* tslint:disable */
/* eslint-disable */
// Generated by Microsoft Kiota
// @ts-ignore
import { createMessageFromDiscriminatorValue, type Message } from '../../../../../../models/index.js';
// @ts-ignore
import { type BaseRequestBuilder, type Parsable, type ParsableFactory, type RequestConfiguration, type RequestInformation, type RequestsMetadata } from '@microsoft/kiota-abstractions';

/**
 * Builds and executes requests for operations under /api/users/{userId}/conversations/{conversationId}/messages
 */
export interface MessagesRequestBuilder extends BaseRequestBuilder<MessagesRequestBuilder> {
    /**
     * @param requestConfiguration Configuration for the request such as headers, query parameters, and middleware options.
     * @returns {Promise<Message[]>}
     */
     get(requestConfiguration?: RequestConfiguration<object> | undefined) : Promise<Message[] | undefined>;
    /**
     * @param requestConfiguration Configuration for the request such as headers, query parameters, and middleware options.
     * @returns {RequestInformation}
     */
     toGetRequestInformation(requestConfiguration?: RequestConfiguration<object> | undefined) : RequestInformation;
}
/**
 * Uri template for the request builder.
 */
export const MessagesRequestBuilderUriTemplate = "{+baseurl}/api/users/{userId}/conversations/{conversationId}/messages";
/**
 * Metadata for all the requests in the request builder.
 */
export const MessagesRequestBuilderRequestsMetadata: RequestsMetadata = {
    get: {
        uriTemplate: MessagesRequestBuilderUriTemplate,
        responseBodyContentType: "application/json",
        adapterMethodName: "sendCollection",
        responseBodyFactory:  createMessageFromDiscriminatorValue,
    },
};
/* tslint:enable */
/* eslint-enable */
