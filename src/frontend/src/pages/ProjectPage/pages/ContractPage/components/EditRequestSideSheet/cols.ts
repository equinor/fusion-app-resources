import { EditableTaleColumns } from "../EditableTable";
import { EditRequest } from '.';

 const columns:EditableTaleColumns<EditRequest>[] = [
    {
        accessor: (item) => item.positionName,
        accessKey: "positionName",
        label: "Position name",
        item: "TextInput"
    }
]

export default columns;