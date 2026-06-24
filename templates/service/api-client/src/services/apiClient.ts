export type ApiClientOptions = {
  baseUrl: string;
  defaultHeaders?: Record<string, string>;
};

export type ApiRequestOptions = {
  headers?: Record<string, string>;
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';
  body?: unknown;
};

export function createApiClient(options: ApiClientOptions) {
  const baseUrl = options.baseUrl.replace(/\/$/, '');

  async function request<TResponse>(
    path: string,
    requestOptions: ApiRequestOptions = {},
  ): Promise<TResponse> {
    const response = await fetch(`${baseUrl}${path}`, {
      method: requestOptions.method ?? 'GET',
      headers: {
        Accept: 'application/json',
        'Content-Type': 'application/json',
        ...options.defaultHeaders,
        ...requestOptions.headers,
      },
      body:
        requestOptions.body === undefined
          ? undefined
          : JSON.stringify(requestOptions.body),
    });

    if (!response.ok) {
      throw new Error(`API request failed with status ${response.status}.`);
    }

    return (await response.json()) as TResponse;
  }

  return {
    request,
    get: <TResponse>(path: string) => request<TResponse>(path),
    post: <TResponse>(path: string, body: unknown) =>
      request<TResponse>(path, { method: 'POST', body }),
  };
}
