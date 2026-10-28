
import { useCallback, useEffect, useState } from 'react';
import { errorMessage, isAbortError } from '../api/client';

interface Result<T> {
  requestId: string;
  data: T | null;
  error: string | null;
}

export function useApiResource<T>(
  resourceKey: string | null,
  fetcher: (signal: AbortSignal) => Promise<T>,
) {
  const [reloadCount, setReloadCount] = useState(0);
  const [result, setResult] = useState<Result<T> | null>(null);

  const requestId = resourceKey === null ? null : `${resourceKey}#${reloadCount}`;

  useEffect(() => {
    if (requestId === null) return;

    const controller = new AbortController();
    fetcher(controller.signal)
      .then((data) => setResult({ requestId, data, error: null }))
      .catch((error: unknown) => {
        if (isAbortError(error)) return;
        setResult({ requestId, data: null, error: errorMessage(error) });
      });

    return () => controller.abort();
  }, [requestId, fetcher]);

  const reload = useCallback(() => setReloadCount((count) => count + 1), []);

  const current = result !== null && result.requestId === requestId ? result : null;

  return {
    data: current?.data ?? null,
    error: current?.error ?? null,
    loading: requestId !== null && current === null,
    reload,
  };
}
