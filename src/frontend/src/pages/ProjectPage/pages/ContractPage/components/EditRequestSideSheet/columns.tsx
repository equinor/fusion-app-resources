import * as React from 'react';
import { DataTableColumn, TextInput, PositionCard } from '@equinor/fusion-components';
import PersonnelRequest from '../../../../../../models/PersonnelRequest';
import BasePositionPicker from '../../../../components/EditContractWizard/components/BasePositionPicker';
import { useContractContext } from '../../../../../../contractContex';

type TextInputColumnProps = {
    item: PersonnelRequest;
    accessor: (request: PersonnelRequest) => string | undefined;
};
type ColumnProps = {
    item: PersonnelRequest;
};

const TextInputColumn: React.FC<TextInputColumnProps> = ({ item, accessor }) => {
    return <TextInput value={accessor(item)} onChange={() => console.log('hei')} />;
};

const BasePositionColumn: React.FC<ColumnProps> = ({ item }) => {
    const { editRequests, setEditRequests } = useContractContext();

    return (
        <BasePositionPicker
            selectedBasePositionId={item.position?.basePosition.id}
            onSelect={basePos => editRequests && setEditRequests(editRequests)}
        />
    );
};
const columns: DataTableColumn<PersonnelRequest>[] = [
    {
        accessor: request => request.person?.name || '',
        key: 'person',
        label: 'Person',
        sortable: true,
        component: request => (
            <TextInputColumn item={request.item} accessor={request => request.person?.name || ''} />
        ),
    },
    {
        accessor: request => request.position?.basePosition?.name || 'TBN',
        key: 'basePosition',
        label: 'Base position',
        sortable: true,
        component: BasePositionColumn,
    },
];

export default columns;
