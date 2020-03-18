import * as React from 'react';

import { useApiClients, useTelemetryLogger, useCurrentContext, Position } from '@equinor/fusion';
import PersonnelRequest from '../../../../../../../models/PersonnelRequest';
import { useAppContext } from '../../../../../../../appContext';

export default (personnelRequests: PersonnelRequest[] | null) => {
    const currentContext = useCurrentContext();

    const [isFetchingPositions, setIsFetchingPositions] = React.useState<boolean>(false);
    const [positionsError, setPositionsError] = React.useState<Error | null>(null);
    const apiClients = useApiClients();
    const telemetryLogger = useTelemetryLogger();
    const { dispatchAppAction, appState } = useAppContext();

    const parentPositionsIds = React.useMemo(() => {
        if (!personnelRequests) {
            return [];
        }
        return personnelRequests.reduce((positionIds: string[], request) => {
            const parentPositionId = request.position?.taskOwner?.positionId;
            if (parentPositionId) {
                return [...positionIds, parentPositionId];
            }
            return positionIds;
        }, []);
    }, [personnelRequests]);

    const cachedPositions = React.useMemo(
        () =>
            appState.positions.data.filter(position =>
                parentPositionsIds.some(parentPositionId => parentPositionId === position.id)
            ),
        [appState, parentPositionsIds]
    );

    const [selectedPositions, setSelectedPositions] = React.useState<Position[]>(cachedPositions);

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
                dispatchAppAction({ verb: 'merge', collection: 'positions', payload: positions });
            } catch (e) {
                telemetryLogger.trackException(e);
                setPositionsError(e);
            }
            setIsFetchingPositions(false);
        },
        [parentPositionsIds]
    );

    React.useEffect(() => {
        if (!currentContext?.externalId) {
            return;
        }
        fetchAllPositions(currentContext.externalId);
    }, [parentPositionsIds]);

    return { selectedPositions, isFetchingPositions, positionsError };
};
