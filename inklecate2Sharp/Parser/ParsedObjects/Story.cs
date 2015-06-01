﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Inklewriter.Parsed
{
	public class Story : FlowBase
    {
        public override FlowLevel flowLevel { get { return FlowLevel.Story; } }

		public Story (List<Parsed.Object> toplevelObjects) : base(null, toplevelObjects)
		{
            // Gather all FlowBase definitions as variable names
            _allKnotAndStitchNames = new List<string>();
            GetAllKnotAndStitchNames (toplevelObjects);
		}

        void GetAllKnotAndStitchNames(List<Parsed.Object> fromContent)
        {
            foreach (var obj in fromContent) {
                var subFlow = obj as FlowBase;
                if (subFlow != null) {
                    _allKnotAndStitchNames.Add (subFlow.dotSeparatedFullName);
                    GetAllKnotAndStitchNames (subFlow.content);
                }
            }
        }

        public override bool HasVariableWithName(string varName)
        {
            if (_allKnotAndStitchNames.Contains (varName)) {
                return true;
            }

            return base.HasVariableWithName (varName);
        }

		public Runtime.Story ExportRuntime()
		{
			// Get default implementation of runtimeObject, which calls ContainerBase's generation method
            var rootContainer = runtimeObject as Runtime.Container;

			// Replace runtimeObject with Story object instead of the Runtime.Container generated by Parsed.ContainerBase
			var runtimeStory = new Runtime.Story (rootContainer);
			runtimeObject = runtimeStory;

			// Now that the story has been fulled parsed into a hierarchy,
			// and the derived runtime hierarchy has been built, we can
			// resolve referenced symbols such as variables and paths.
			// e.g. for paths " -> knotName --> stitchName" into an INKPath (knotName.stitchName)
			// We don't make any assumptions that the INKPath follows the same
			// conventions as the script format, so we resolve to actual objects before
			// translating into an INKPath. (This also allows us to choose whether
			// we want the paths to be absolute)
			ResolveReferences (this);

			// Don't successfully return the object if there was an error
			if (_criticalError) {
				return null;
			}

			return runtimeStory;
		}

        // Initialise all read count variables for every knot and stitch name
        // TODO: This seems a bit overkill, to mass-generate a load of variable assignment
        // statements. Could probably just include a bespoke "initial variable state" in
        // the story, or even just a list of knots/stitches that the story automatically
        // initialises.
        protected override void OnRuntimeGenerationDidStart(Runtime.Container container)
        {
            container.AddContent (Runtime.ControlCommand.EvalStart());

            foreach (string flowName in _allKnotAndStitchNames) {
                container.AddContent (new Runtime.Number (0));
                container.AddContent (new Runtime.VariableAssignment (flowName, true));
            }

            container.AddContent (Runtime.ControlCommand.EvalEnd());

            // FlowBase handles argument variable assignment and read count updates
            base.OnRuntimeGenerationDidStart(container);
        }

		public override void Error(string message, Parsed.Object source)
		{
            var sb = new StringBuilder ();
            sb.Append ("ERROR: ");
            sb.Append (message);
            if (source != null && source.debugMetadata != null && source.debugMetadata.lineNumber >= 1 ) {
                sb.Append (" on line "+source.debugMetadata.lineNumber);
            }
            Console.WriteLine (sb.ToString());
			_criticalError = true;
		}

		private bool _criticalError;
        List<string> _allKnotAndStitchNames;
	}
}

