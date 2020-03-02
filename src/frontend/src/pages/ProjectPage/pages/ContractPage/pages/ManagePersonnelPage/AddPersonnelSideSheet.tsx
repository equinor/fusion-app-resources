import * as React from 'react';
import { ModalSideSheet, DataTable } from '@equinor/fusion-components';
import AddPersonnelFormColumns from './AddPersonnelFormColumns';
import Personnel from '../../../../../../models/Personnel';
import Person from '../../../../../../models/Person';

type AddPersonnelToSideSheetProps = {
  isOpen: boolean;
  selectedPersonnel:Personnel[]
  setIsOpen: (state:boolean) => void;
}

const AddPersonellSideSheet: React.FC<AddPersonnelToSideSheetProps> = ({isOpen,setIsOpen,selectedPersonnel}) => {
  return(
    <ModalSideSheet
      header="Add Person"
      show={isOpen}
      onClose={() => {
        setIsOpen(false);
      }}
      isResizable
      minWidth={300}
    >
      <AddPersonnelForm personnelForEdit={selectedPersonnel} />
    </ModalSideSheet>
  )
}

type AddPersonnelFormProps = {
  personnelForEdit?:Personnel[]
}

const AddPersonnelForm: React.FC<AddPersonnelFormProps> = ({personnelForEdit}) => {
  const [personnel,setPersonnel] = React.useState<Person[]>(   
    personnelForEdit 
      ? personnelForEdit 
      : [{name:"",phoneNumber:"",mail:"",jobTitle:""}] 
  )

  const columns = React.useMemo(() => AddPersonnelFormColumns(), []);



    console.log(personnelForEdit)
    console.log(personnel)

  return(
    <DataTable 
      columns={columns}
      data={personnel}
      isFetching={false}
      rowIdentifier={'name'}
   />
  )
}


export default AddPersonellSideSheet



