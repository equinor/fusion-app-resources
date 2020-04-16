import { Position, useApiClients, useCurrentContext } from '@equinor/fusion';
import * as React from 'react';
import { useAppContext } from '../../../../../../../appContext';

export default (positionId?: string) => {
    const [position, setPosition] = React.useState<Position | null>(null);
    const [isFetchingPosition, setIsFetchingPosition] = React.useState<boolean>(false);
    const [positionError, setPositionError] = React.useState<Error | null>(null);

    const { dispatchAppAction, appState } = useAppContext();
    const apiClients = useApiClients();

    const currentContext = useCurrentContext();
    const currentOrgProject = currentContext as any;

    const getCachedPosition = React.useCallback(() => {
        return appState.positions.data.find((pos) => pos.id === positionId);
    }, [positionId, appState]);

    const fetchPositionAsync = React.useCallback(async (projectId: string, posId: string) => {
        setIsFetchingPosition(true);
        setPositionError(null);
        try {
            const response = await apiClients.org.getPositionAsync(projectId, posId);
            setPosition(response.data);
            dispatchAppAction({
                verb: 'merge',
                collection: 'positions',
                payload: [response.data],
            });
        } catch (e) {
            console.error(e);
            setPositionError(e);
        } finally {
            setIsFetchingPosition(false);
        }
    }, []);

    React.useEffect(() => {
        const cachedPosition = getCachedPosition();
        if (cachedPosition) {
            setPosition(cachedPosition);
        }
        const projectId = currentOrgProject?.externalId;

        if (projectId && positionId) {
            fetchPositionAsync(projectId as string, positionId);
        }
    }, [positionId, currentOrgProject]);

    return { position, isFetchingPosition, positionError };
};
