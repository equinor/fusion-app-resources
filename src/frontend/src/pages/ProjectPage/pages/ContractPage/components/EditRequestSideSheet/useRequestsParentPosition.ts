import * as React from 'react';

import { useApiClients, useTelemetryLogger, useCurrentContext, Position } from '@equinor/fusion';
import PersonnelRequest from '../../../../../../models/PersonnelRequest';

export default (personnelRequests: PersonnelRequest[] | null) => {
    const currentContext = useCurrentContext();
    const currentOrgProject = currentContext as any;

    const [selectedPositions, setSelectedPositions] = React.useState<Position[] | null>(null);
    const [isFetchingPositions, setIsFetchingPositions] = React.useState<boolean>(false);
    const [positionsError, setPositionsError] = React.useState<Error | null>(null);
    const apiClients = useApiClients();
    const telemetryLogger = useTelemetryLogger();

    const parentPositionsIds = React.useMemo(() => {
        if (!personnelRequests) {
            return [];
        }
        return personnelRequests.reduce((positionIds: string[], request) => {
            const parentPositionId = request.position?.instances.find(i => i.parentPositionId)
                ?.parentPositionId;
            if (parentPositionId) {
                return [...positionIds, parentPositionId];
            }
            return positionIds;
        }, []);
    }, [personnelRequests]);

    const fetchAllPositions = React.useCallback(
        async (projectId: string) => {
            try {
                setIsFetchingPositions(true);
                const positionsPromises = parentPositionsIds.map(positionId => {
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
        [parentPositionsIds]
    );

    React.useEffect(() => {
        if (!currentOrgProject) {
            return;
        }
        fetchAllPositions(currentOrgProject);
    }, [parentPositionsIds]);

    return { selectedPositions, isFetchingPositions, positionsError };
};
