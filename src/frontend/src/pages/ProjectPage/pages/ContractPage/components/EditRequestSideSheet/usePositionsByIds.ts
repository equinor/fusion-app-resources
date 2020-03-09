import * as React from 'react';

import { useApiClients, useTelemetryLogger, useCurrentContext, Position } from '@equinor/fusion';

export default (positionIds: string[]) => {
    const currentContext = useCurrentContext();
    const currentOrgProject = currentContext as any;

    const [selectedPositions, setSelectedPositions] = React.useState<Position[] | null>(null);
    const [isFetchingPositions, setIsFetchingPositions] = React.useState<boolean>(false);
    const [positionsError, setPositionsError] = React.useState<Error | null>(null);
    const apiClients = useApiClients();
    const telemetryLogger = useTelemetryLogger();

    const fetchAllPositions = React.useCallback(
        async (projectId: string) => {
            try {
                setIsFetchingPositions(true);
                const positionsPromises = positionIds.map(positionId => {
                    return apiClients.org.getPositionAsync(projectId, positionId);
                });
                const positionsResponse = await Promise.all(positionsPromises);
                const positions = positionsResponse.map(position => position.data);
                setSelectedPositions(positions);
            } catch (e) {
                telemetryLogger.trackException(e);
                setPositionsError(e);
            }
            setIsFetchingPositions(false);
        },
        [positionIds]
    );

    React.useEffect(() => {
        if (!currentOrgProject) {
            return;
        }
        fetchAllPositions(currentOrgProject);
    }, [positionIds]);

    return { selectedPositions, isFetchingPositions, positionsError };
};
