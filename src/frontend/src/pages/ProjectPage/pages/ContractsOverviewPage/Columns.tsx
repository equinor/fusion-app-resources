import { DataTableColumn, styling } from "@equinor/fusion-components";
import Contract from '../../../../models/contract';
import ContractLinkColumn from './components/ContractLinkColumn';
import PositionColumn from '../../components/PositionColumn';

const createColumns = (): DataTableColumn<Contract>[] => [
    {
        accessor: 'contractNumber',
        key: 'number',
        label: 'Contract no.',
        sortable: true,
        component: ({ item }) => (
                <ContractLinkColumn data-cy="contract-id" contractId={item.id}>{item.contractNumber}</ContractLinkColumn>
        ),
    },
    {
        accessor: 'name',
        key: 'name',
        label: 'Name',
        sortable: true,
        width: styling.grid(20),
        component: ({ item }) => (
            <ContractLinkColumn contractId={item.id}>{item.name}</ContractLinkColumn>
        ),
    },
    {
        accessor: contract => contract.companyRep?.name || '',
        key: 'companyRepPositionId',
        label: 'Equinor company rep',
        component: ({ item }) => <PositionColumn data-cy='company-rep' position={item.companyRep} />,
        sortable: true,
    },
    {
        accessor: contract => contract.contractResponsible?.name || '',
        key: 'contractResponsiblePositionId',
        label: 'Equinor contract rep',
        component: ({ item }) => <PositionColumn data-cy='contract-rep' position={item.contractResponsible} />,
        sortable: true,
    },
    {
        accessor: contract => contract.company?.name || 'no company',
        key: 'company',
        label: 'Company',
        sortable: true,
    },
    {
        accessor: contract => contract.externalContractResponsible?.name || '',
        key: 'externalContractResponsiblePositionId',
        label: 'External company rep',
        component: ({ item }) => <PositionColumn position={item.externalContractResponsible} />,
        sortable: true,
    },
    {
        accessor: contract => contract.externalCompanyRep?.name || '',
        key: 'externalCompanyRepPositionId',
        label: 'External contract rep',
        component: ({ item }) => <PositionColumn position={item.externalCompanyRep} />,
        sortable: true,
    },
];

export default createColumns;