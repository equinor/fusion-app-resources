import { useState, useEffect } from 'react';
import { useApiClients, BasePosition, combineUrls, useTelemetryLogger } from '@equinor/fusion';

export type useBasePositionsContext = {
  basePositions: BasePosition[];
  isFetchingBasePositions: Boolean;
  basePositionsError: Error | null
}

const useBasePositionsContext = (): useBasePositionsContext => {
  const [basePositions, setBasePositions] = useState<BasePosition[]>([]);
  const [isFetchingBasePositions, setIsFetchingBasePositions] = useState(false);
  const [basePositionsError, setBasePositionsError] = useState<Error | null>(null);

  const apiClients = useApiClients();
  const telemetryLogger = useTelemetryLogger();

  const fetchBasePositions = async () => {
    setIsFetchingBasePositions(true);
    setBasePositionsError(null);

    try {
      const response = await apiClients.org.getAsync<BasePosition[]>(
        combineUrls('positions', "basepositions?$filter=projectType eq 'PRD-Contracts'")
      );
      setBasePositions(response.data);

    } catch (e) {
      telemetryLogger.trackException(e);
      setBasePositionsError(e);
    }

    setIsFetchingBasePositions(false);
  };

  useEffect(() => {
    fetchBasePositions();
  }, []);

  return {
    basePositions,
    isFetchingBasePositions,
    basePositionsError,
  };
};

export default useBasePositionsContext;
