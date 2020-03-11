import * as React from "react";
import PersonnelRequest from '../../../../../../../models/PersonnelRequest';
import { ModalSideSheet } from '@equinor/fusion-components';
import RequestDetails from './RequestDetails';

type RequestDetailsSideSheetProps = {
    request: PersonnelRequest | null;
}

const RequestDetailsSideSheet: React.FC<RequestDetailsSideSheetProps> = ({request}) => {
    const [currentRequest, setCurrentRequest] = React.useState<PersonnelRequest | null>(null);

    const showSideSheet = React.useMemo(() => request !== null, [currentRequest]);

    const onClose = React.useCallback(() => {
        setCurrentRequest(null)
    },[setCurrentRequest])

    React.useEffect(() => {
        setCurrentRequest(request)
    },[request]);

    if(!request) {
        return null;
    }

    return(
        <ModalSideSheet show={showSideSheet} header={request.position?.basePosition.name || ""} onClose={onClose} >
            <RequestDetails request={request}/>
        </ModalSideSheet>
    )
}
export default RequestDetailsSideSheet;