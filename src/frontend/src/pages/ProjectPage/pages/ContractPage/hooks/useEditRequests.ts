import * as React from 'react';
import PersonnelRequest from '../../../../../models/PersonnelRequest';

export default () => {
    const [editRequests, setEditRequests] = React.useState<PersonnelRequest[] | null>(null);
    return { editRequests, setEditRequests };
};
