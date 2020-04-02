import * as React from 'react';
import PersonnelRequest from '../../../../../../../models/PersonnelRequest';
import { useHistory } from '@equinor/fusion';
import { parseQueryString } from '../../../../../../../api/utils';

export default (requests: PersonnelRequest[] | null) => {
    const history = useHistory();
    const [currentRequest, setCurrentRequest] = React.useState<PersonnelRequest | null>();

    React.useEffect(() => {
        if (!requests) {
            return;
        }
        const params = parseQueryString(history.location.search);
        const selectedRequest = requests.find(r => r.id === params.requestId);
        setCurrentRequest(selectedRequest || null);
    }, [history.location.search, requests]);

    React.useEffect(() => {
        if (currentRequest === null) {
            history.push({
                pathname: history.location.pathname,
                search: '',
            });
        }
    }, [currentRequest]);

    return { currentRequest: currentRequest || null, setCurrentRequest };
};
