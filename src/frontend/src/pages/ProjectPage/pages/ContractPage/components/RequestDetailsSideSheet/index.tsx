import * as React from 'react';
import PersonnelRequest from '../../../../../../models/PersonnelRequest';
import { ModalSideSheet } from '@equinor/fusion-components';
import RequestDetails from './RequestDetails';
import useCurrentRequest from './hooks/useCurrentRequest';

type RequestDetailsSideSheetProps = {
    requests: PersonnelRequest[] | null;
};

const RequestDetailsSideSheet: React.FC<RequestDetailsSideSheetProps> = ({ requests }) => {
    const { currentRequest, setCurrentRequest } = useCurrentRequest(requests);

    const showSideSheet = React.useMemo(() => currentRequest !== null, [currentRequest]);

    const onClose = React.useCallback(() => {
        setCurrentRequest(null);
    }, [setCurrentRequest]);

    if (!currentRequest) {
        return null;
    }

    return (
        <ModalSideSheet
            show={showSideSheet}
            header={currentRequest.position?.basePosition?.name || ''}
            onClose={onClose}
        >
            <RequestDetails request={currentRequest} />
        </ModalSideSheet>
    );
};
export default RequestDetailsSideSheet;
