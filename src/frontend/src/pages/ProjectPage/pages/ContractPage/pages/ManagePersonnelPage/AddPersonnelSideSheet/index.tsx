import * as React from 'react';
import { ModalSideSheet, Button, Spinner } from '@equinor/fusion-components';
import AddPersonnelFormColumns from './AddPersonnelFormColumns';
import Personnel from '../../../../../../../models/Personnel';
import Person from '../../../../../../../models/Person';
import * as uuid from "uuid/v1";
import * as classNames from 'classnames'
import * as styles from './styles.less'
import { useComponentDisplayClassNames } from '@equinor/fusion';
import { generateRowTemplate, generateColumnTemplate } from './utils';
import Header from './Header';

type AddPersonnelToSideSheetProps = {
  isOpen: boolean;
  selectedPersonnel:Personnel[]
  setIsOpen: (state:boolean) => void;
}


const AddPersonnelSideSheet: React.FC<AddPersonnelToSideSheetProps> = ({isOpen,setIsOpen,selectedPersonnel}) => {
  return(
    <ModalSideSheet
      header="Add Person"
      show={isOpen}
      onClose={() => {
        setIsOpen(false);
      }}
      safeClose
      safeCloseTitle={`Close Add Person? Unsaved changes will be lost.
                      `}
      isResizable
      minWidth={640}
    >
      <AddPersonnelForm personnelForEdit={selectedPersonnel} />
    </ModalSideSheet>
  )
}

type AddPersonnelFormProps = {
  personnelForEdit?:Personnel[]
}


const AddPersonnelForm: React.FC<AddPersonnelFormProps> = ({personnelForEdit}) => {
  const [saveInProgress,setSaveInProgress] = React.useState<Boolean>(false);
  const [personnel,setPersonnel] = React.useState<Person[]>(   
    personnelForEdit?.length 
      ? personnelForEdit.map(p => ({...p,personnelId:uuid()})) //TODO: Remove UUID when it has been added to the API.
      : [{personnelId:uuid(), name:"",phoneNumber:"",mail:"",jobTitle:""}] 
  )

  const headers = ["Name","Mail","Phone"]
  const rowTemplate = generateRowTemplate(headers);
  const columnTemplate = generateColumnTemplate(headers);
  const containerClassNames = classNames(styles.container, useComponentDisplayClassNames(styles));
  const  savePersonnelChangesAsync = async () =>{
    setSaveInProgress(true);
    
  }

  if(saveInProgress)
    return(<Spinner title={"they se me spinning they hatin"}> SPINNIN ! </Spinner>)

  return(
    <>
      <div
      style={{
        display: "inline",
        position: "absolute",
        top: "0%",
        right: "16px",
        padding: "8px",
      }}
      >
        <Button outlined onClick = {()=> {setPersonnel([...personnel,{personnelId:uuid(), name:"",phoneNumber:"",mail:"",jobTitle:""}])}} > + Add Person </Button>
        <Button outlined onClick = {()=> {savePersonnelChangesAsync()} }> Create </Button>
      </div>
      <div className={containerClassNames}>
        <div className={styles.table}
            style={{ gridTemplateColumns: columnTemplate, gridTemplateRows: rowTemplate }}
        >
          <Header headers={headers} />
          <table >
            <AddPersonnelFormColumns personnel={personnel} setPersonnel={setPersonnel} />
          </table>
        </div>
      </div>
    </>
  )
}

export default AddPersonnelSideSheet



