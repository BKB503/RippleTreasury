export class ApiError extends Error {
  correlationId: string;
  constructor(message: string, correlationId: string) {
    super(message);
    this.correlationId = correlationId;
  }
}
export async function api<T>(path: string, options?: RequestInit): Promise<T> {
  const correlationId = crypto.randomUUID();
  const response = await fetch("/api/v1" + path, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      "X-Correlation-ID": correlationId,
      ...options?.headers,
    },
  });
  if (!response.ok) {
    const problem = await response
      .json()
      .catch(() => ({ title: "Service unavailable" }));
    throw new ApiError(
      problem.title ?? "Request failed",
      problem.correlationId ??
        response.headers.get("X-Correlation-ID") ??
        correlationId,
    );
  }
  return response.json();
}
