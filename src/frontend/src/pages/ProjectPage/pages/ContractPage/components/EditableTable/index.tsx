import { useState, useRef, useCallback, useMemo, useEffect } from 'react';
import styles from './styles.less';
import Taskbar from './Taskbar';
import TableTextInput from './components/TableTextInput';
import TableBasePosition from './components/TableBasePositionPicker';
import TablePositionPicker from './components/TablePositionPicker';
import TablePersonPicker from './components/TablePersonPicker';
import TablePersonnelPicker from './components/TablePersonnelPicker';
import TableDatePicker from './components/TableDatePicker';
import Personnel from '../../../../../../models/Personnel';
import { BasePosition } from '@equinor/fusion';
import { ReadonlyCollection } from '../../../../../../reducers/utils';
import TableTextEditor from './components/TableTextArea';
import SelectionCell from '../../pages/ManagePersonnelPage/components/SelectionCell';
import classNames from 'classnames';
import { useTooltipRef } from '@equinor/fusion-components';
import TableToolbar from './components/TableToolbar';

export type EditableTableComponentState = {
    personnel?: ReadonlyCollection<Personnel>;
    basePositions?: ReadonlyCollection<BasePosition>;
};

export type EditableTaleColumnItem =
    | 'TextInput'
    | 'PersonPicker'
    | 'PositionPicker'
    | 'BasePositionPicker'
    | 'PersonnelPicker'
    | 'DatePicker'
    | 'TextArea';

export type EditableTaleColumn<T> = {
    accessor: (item: T) => any;
    item: EditableTaleColumnItem;
    label: string;
    accessKey: keyof T;
};

type EditableTableProps<T> = {
    formState: T[];
    setFormState: (newState: T[]) => void;
    columns: EditableTaleColumn<T>[];
    createDefaultState: () => T[];
    rowIdentifier: keyof T;
    isFetching?: boolean;
    componentState?: EditableTableComponentState;
    createCopyState?: (items: T) => T;
    onItemChange?: (item: T, accessKey: keyof T) => T;
};

function EditableTable<T>({
    formState,
    setFormState,
    columns,
    createDefaultState,
    rowIdentifier,
    isFetching,
    componentState,
    createCopyState,
    onItemChange
}: EditableTableProps<T>) {
    const [selectedItems, setSelectedItems] = useState<T[]>([]);
    const [activeTableCell, setActiveTableCell] = useState<HTMLTableCellElement | null>(null);
    const tableContainerRef = useRef<HTMLDivElement | null>(null);

    const onChange = (key: any, accessKey: keyof T, value: any) => {
        const updatedPersons = [...formState].map(stateItem => {
            const nullValue = typeof stateItem[accessKey] === 'string' ? '' : null;

            if(stateItem[rowIdentifier] !== key) {
                return stateItem;
            }

            const mutatedItem = { ...stateItem, [accessKey]: value || nullValue };
            if(onItemChange) {
                return onItemChange(mutatedItem, accessKey);
            }

            return mutatedItem;
        });
        setFormState(updatedPersons);
    };

    const onAddItem = useCallback(() => {
        const newStateItem = createDefaultState();
        setFormState([...formState, ...newStateItem]);
    }, [formState, createDefaultState]);

    const onRemoveItems = useCallback(
        (items: T[]) => {
            const newFormState = formState.filter(
                stateItem => !items.some(item => item[rowIdentifier] === stateItem[rowIdentifier])
            );
            const newSelectedItems = selectedItems.filter(
                stateItem => !items.some(item => item[rowIdentifier] === stateItem[rowIdentifier])
            );
            setFormState(newFormState);
            setSelectedItems(newSelectedItems);
        },
        [formState, rowIdentifier, selectedItems]
    );

    const onCopyItems = useCallback(
        (items: T[]) => {
            if (!createCopyState) {
                return;
            }
            const copyItems = items.map(item => createCopyState(item));
            setFormState([...formState, ...copyItems]);
        },
        [setFormState, createCopyState, formState]
    );

    const getTableComponent = useCallback(
        (column: EditableTaleColumn<T>, item: T) => {
            const defaultProps = {
                item,
                accessKey: column.accessKey,
                accessor: column.accessor,
                onChange,
                rowIdentifier,
                isFetching,
                columnLabel: column.label,
            };
            switch (column.item) {
                case 'TextInput':
                    return <TableTextInput {...defaultProps} />;
                case 'BasePositionPicker':
                    return (
                        <TableBasePosition
                            {...defaultProps}
                            componentState={componentState?.basePositions}
                        />
                    );
                case 'PositionPicker':
                    return <TablePositionPicker {...defaultProps} />;
                case 'PersonPicker':
                    return <TablePersonPicker {...defaultProps} />;
                case 'PersonnelPicker':
                    return (
                        <TablePersonnelPicker
                            {...defaultProps}
                            componentState={componentState?.personnel}
                        />
                    );
                case 'DatePicker':
                    return <TableDatePicker {...defaultProps} />;
                case 'TextArea':
                    return <TableTextEditor {...defaultProps} />;
                default:
                    return null;
            }
        },
        [onChange, rowIdentifier, isFetching]
    );

    const onItemSelectChange = useCallback(
        (item: T) => {
            if (selectedItems && selectedItems.some(i => i === item)) {
                setSelectedItems(selectedItems.filter(i => i !== item));
            } else {
                setSelectedItems([...(selectedItems || []), item]);
            }
        },
        [selectedItems]
    );

    const isAllSelected = useMemo(() => selectedItems.length === formState.length, [
        selectedItems,
        formState,
    ]);

    const selectableTooltipRef = useTooltipRef(
        isAllSelected ? 'Unselect all' : 'Select all',
        'above'
    );

    const onSelectAll = useCallback(() => {
        setSelectedItems(selectedItems.length === formState.length ? [] : formState);
    }, [formState, selectedItems]);

    const scrollToTableCell = useCallback(
        (tableCell: HTMLTableCellElement | null) => {
            if (!tableContainerRef.current || !tableCell) {
                return;
            }
            const header = tableContainerRef.current;

            if (header.scrollWidth === header.offsetWidth) {
                return;
            }
            header.scrollTo(
                tableCell.offsetLeft - header.offsetWidth / 2 + tableCell.offsetWidth / 2,
                0
            );
        },
        [tableContainerRef]
    );

    useEffect(() => {
        scrollToTableCell(activeTableCell);
    }, [activeTableCell]);

    const tableHeader = useMemo(() => {
        return (
            <thead>
                <tr>
                    <th
                        className={classNames(styles.header, styles.selectionCell)}
                        ref={selectableTooltipRef}
                    >
                        <SelectionCell
                            isSelected={
                                !!selectedItems && selectedItems.length === formState.length
                            }
                            onChange={onSelectAll}
                            indeterminate={
                                !!selectedItems &&
                                selectedItems.length > 0 &&
                                selectedItems.length !== formState.length
                            }
                        />
                    </th>
                    <th className={classNames(styles.header, styles.toolbarCell)} />

                    {columns.map(column => (
                        <th className={styles.header} key={column.label + 'header'}>
                            {column.label}
                        </th>
                    ))}
                </tr>
            </thead>
        );
    }, [columns, selectedItems, formState]);

    const getRowBodySelectedState = useCallback(
        (stateItem: T) =>
            !!selectedItems &&
            selectedItems.some(
                selectedItem => selectedItem[rowIdentifier] === stateItem[rowIdentifier]
            ),
        [selectedItems]
    );

    const tableBody = useMemo(() => {
        return (
            <tbody>
                {formState.map((stateItem, index) => (
                    <tr key={`item-${index}`}>
                        <td className={classNames(styles.tableRowCell, styles.selectionCell)}>
                            <SelectionCell
                                isSelected={getRowBodySelectedState(stateItem)}
                                onChange={() => onItemSelectChange(stateItem)}
                            />
                        </td>
                        <td className={classNames(styles.tableRowCell, styles.toolbarCell)}>
                            <TableToolbar
                                onRemove={() => onRemoveItems([stateItem])}
                                onCopy={() => onCopyItems([stateItem])}
                            />
                        </td>
                        {columns.map(column => (
                            <td
                                key={column.accessKey.toString()}
                                className={styles.tableRowCell}
                                onClick={e => {
                                    setActiveTableCell(e.currentTarget);
                                }}
                            >
                                {getTableComponent(column, stateItem)}
                            </td>
                        ))}
                    </tr>
                ))}
            </tbody>
        );
    }, [columns, formState, selectedItems, setActiveTableCell]);

    return (
        <div className={styles.editableTable}>
            <Taskbar
                onAddItem={onAddItem}
                selectedItems={selectedItems}
                onRemoveItems={onRemoveItems}
                onCopyItems={() => onCopyItems(selectedItems)}
            />
            <div className={styles.container} ref={tableContainerRef}>
                <table className={styles.table}>
                    {tableHeader}
                    {tableBody}
                </table>
            </div>
        </div>
    );
}

export default EditableTable;
