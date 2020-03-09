import * as React from 'react';
import { DataTableColumn, TextInput, PositionCard, PersonPicker } from '@equinor/fusion-components';
import PersonnelRequest from '../../../../../../models/PersonnelRequest';
import BasePositionPicker from '../../../../components/EditContractWizard/components/BasePositionPicker';
import { useContractContext } from '../../../../../../contractContex';
import CreatePersonnelRequest from '../../../../../../models/CreatePersonnelRequest';

type TextInputColumnProps = {
    item: PersonnelRequest;
    accessor: (request: PersonnelRequest) => string | undefined;
};
type ColumnProps = {
    item: CreatePersonnelRequest;
};

const TextInputColumn: React.FC<TextInputColumnProps> = ({ item, accessor }) => {
    return <TextInput value={accessor(item)} onChange={() => console.log('hei')} />;
};

// const PersonPickerColumn: React.FC<ColumnProps> = ({ item }) => {
//     const person = item.person
//     return (
//         <PersonPicker
//             selectedPerson={selectedPerson}
//             initialPerson={selectedPerson}
//             onSelect={onPersonSelect}
//         />
//     );
// };
const BasePositionColumn: React.FC<ColumnProps> = ({ item }) => {
    const { editRequests, setEditRequests } = useContractContext();
    const basePositionId = (item.position && item.position.basePosition?.id) || undefined;
    return (
        <BasePositionPicker
            selectedBasePositionId={basePositionId}
            onSelect={basePos => editRequests && setEditRequests(editRequests)}
        />
    );
};
const columns: DataTableColumn<CreatePersonnelRequest>[] = [
    {
        accessor: request => request.position?.basePosition?.name || 'TBN',
        key: 'basePosition',
        label: 'Base position',
        sortable: true,
        component: BasePositionColumn,
    },
];

export default columns;
